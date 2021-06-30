using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Drawing;
using DBModel;
using System.Data.Entity;

namespace Diary.Pages
{
    public class IndexModel : PageModel
    {
        public DateTime timeNow = DateTime.Now;
        public DateTime dateFrom;
        public DateTime dateTo;
        public bool allowTasks = true;
        public bool allowNotes = true;
        public bool allowMeetings = true;

        public string message = "";
        public DiaryContext context = new DiaryContext();

        public IEnumerable<DBModel.Entry> entries;
        public IEnumerable<DBModel.Meeting> meetings;
        public IEnumerable<DBModel.Task> tasks;
        public IEnumerable<DBModel.Note> notes;

        private void RestoreFilters(DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            //Восстанавливаем фильтры
            int aMeetings = (allowMeetings != null) ? allowMeetings.Value : 1;
            int aNotes = (allowNotes != null) ? allowNotes.Value : 1;
            int aTasks = (allowTasks != null) ? allowTasks.Value : 1;

            DateTime dFrom = DateTime.Now;
            DateTime dTo = DateTime.Now.AddDays(1);
            if (dateFrom == null || dateTo == null || dateFrom.Value.Year < 2010 || dateTo.Value.Year < 2010 ||
               dateFrom > dateTo)
            {
            }
            else
            {
                dFrom = dateFrom.Value;
                dTo = dateTo.Value;
            }

            OnGet(dFrom, dTo, aNotes, aMeetings, aTasks);
        }



        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        private bool RangeCollide(DateTime dF, DateTime dT, DateTime dA, DateTime dB)
        {
            //a.start <= b.end AND a.end >= b.start
            if (dF <= dB && dT >= dA)
                return true;
            else
                return false;
        }

        public void OnGet(DateTime dateFrom, DateTime dateTo, int? allowNotes, int? allowMeetings, int? allowTasks, string? searchPattern="")
        {
            if (searchPattern==null)
                searchPattern = "";

            if (allowMeetings != null)
                this.allowMeetings = allowMeetings > 0 ? true : false; //Такая сложная конструкция на случай, если я найду способ сохранять фильтры при переключении по датам.
            if (allowNotes != null)
                this.allowNotes = allowNotes > 0 ? true : false;
            if (allowTasks != null)
                this.allowTasks = allowTasks > 0 ? true : false;

            if (dateFrom == null || dateTo == null || dateFrom.Year < 2010 || dateTo.Year < 2010 ||
               dateFrom > dateTo)
            {
                //Если данные некорректны
                this.dateFrom = timeNow.Date;
                this.dateTo = this.dateFrom.AddDays(1);
            }
            else
            {
                this.dateFrom = dateFrom;
                this.dateTo = dateTo;
            }

            //Достаём информацию из базы данных
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DiaryContext>());
            //DiaryContext context = new DiaryContext();


            //...но сначала проверяем, есть ли она
            if (!context.Entries.Any())
            {
                //Если её нет, то ничего не делаем
            }
            else
            {
                //Иначе же применяем фильтры и достаём
                notes = from n in context.Notes
                        where this.allowNotes && n.dateStart <= this.dateTo && n.dateStart >= this.dateFrom && (n.Subject.ToLower().Contains(searchPattern.ToLower()) || (searchPattern=="") || n.Details.ToLower().Contains(searchPattern.ToLower()))
                        select n;
                meetings = from m in context.Meetings
                           where this.allowMeetings && m.dateStart <= this.dateTo && m.dateFinish >= this.dateFrom && (m.Subject.ToLower().Contains(searchPattern.ToLower()) || (searchPattern == "") || m.Details.ToLower().Contains(searchPattern.ToLower()))
                           select m;
                tasks = from t in context.Tasks
                        where this.allowTasks && t.dateStart <= this.dateTo && t.dateFinish >= this.dateFrom && (t.Subject.ToLower().Contains(searchPattern.ToLower()) || (searchPattern == "") || t.Details.ToLower().Contains(searchPattern.ToLower()))
                        select t;
                var list = notes.Concat(meetings as IEnumerable<Entry>).Concat(tasks).ToList();
                list.Sort(delegate (Entry x, Entry y)
                {
                    if (x.dateStart == null && y.dateStart == null) return 0;
                    else if (x.dateStart == null) return -1;
                    else if (y.dateStart == null) return 1;
                    else if (x.dateStart > y.dateStart) return 1;
                    else return -1;
                });
                entries = list;
                    

                ;
            }

        }

        public void OnPostAdd(int? entryType, string? entrySubject, string? entryDetails, DateTime? entryDateFrom, DateTime? entryDateTo, string? entryPlace)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DiaryContext>());
            //Проверяем целостность данных и, если надо, исправляем их
            if (entryDateFrom == null)
            {
                StatusCode(400);
                return;
            }
            if (entrySubject != null && entrySubject.Length > 200)
                entrySubject = entrySubject.Substring(0, 2000);

            if (entryDetails!=null && entryDetails.Length>2000)
                entryDetails = entryDetails.Substring(0, 2000);

            if (entryDateTo!=null && entryDateTo.Value<entryDateFrom)
            {
                entryDateTo = null; //Потом оно исправится на нужное
            }


            if (entryType == 1)
            {
                Meeting entry = new Meeting();
                entry.Done = false;
                entry.Subject = entrySubject;
                entry.Details = entryDetails;
                entry.dateStart = entryDateFrom.Value;
                entry.dateFinish = (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value.AddMinutes(30);
                entry.Place = entryPlace;
                context.Meetings.Add(entry);
            }
            else if (entryType == 2)
            {
                DBModel.Task entry = new DBModel.Task();
                entry.Done = false;
                entry.Subject = entrySubject;
                entry.Details = entryDetails;
                entry.dateStart = entryDateFrom.Value;
                entry.dateFinish = (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value.AddMinutes(30);
                context.Tasks.Add(entry);
            }
            else
            {
                Note entry = new Note();
                entry.Done = false;
                entry.Subject = entrySubject;
                entry.Details = entryDetails;
                entry.dateStart = entryDateFrom.Value;
                context.Notes.Add(entry);
            }

            // Сохранить изменения в БД
            context.SaveChanges();

            RestoreFilters(entryDateFrom, entryDateTo, (this.allowNotes? 1: 0), (this.allowNotes ? 1 : 0), (this.allowNotes ? 1 : 0));

            /*OnGet(entryDateFrom.Value.Date,
                (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value.Date.AddDays(1),
                (this.allowNotes) ? 1 : 0,
                (this.allowMeetings) ? 1 : 0,
                this.allowTasks ? 1 : 0);*/
        }

        public void OnPostDelete(int? id, DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DiaryContext>());
            //DiaryContext context = new DiaryContext();

            var entries = (from e in context.Entries
                           where e.ID == id
                           select e);

            if (entries.Count() == 0)
            {
                message = "Запись с ID = " + id + " не найдена!"; //Что очень странно
                return;
            }

            Entry entry = entries.First();

            context.Entries.Remove(entry);

            context.SaveChanges();

            RestoreFilters(dateFrom, dateTo, allowNotes, allowMeetings, allowTasks);
        }

        public void OnPostEdit(int? id, string? newSubject, string? newDetails, DateTime? newDateFrom, DateTime? newDateTo, string? newPlace, /*А теперь старые установки фильтров*/ DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DiaryContext>());
            //DiaryContext context = new DiaryContext();

            var entries = (from e in context.Entries
                           where e.ID == id
                           select e);

            if (entries.Count() == 0)
            {
                message = "Запись с ID = " + id + " не найдена!"; //Что очень странно
                return;
            }

            Entry entry = entries.First();

            string newSubjectFinal = (newSubject != null) ? newSubject : entry.Subject;
            string newDetailsFinal = (newDetails != null) ? newDetails : entry.Details;
            DateTime newDateFromFinal = (newDateFrom != null) ? newDateFrom.Value : entry.dateStart;
            entry.Subject = newSubjectFinal;
            entry.Details = newDetailsFinal;
            entry.dateStart = newDateFromFinal;

            if (entry is Meeting)
            {
                if (newDateTo != null)
                {
                    ((Meeting)entry).dateFinish = newDateTo.Value;
                }
                ((Meeting)entry).Place = (newPlace != null) ? newPlace : ((Meeting)entry).Place;
            }

            else if (entry is DBModel.Task)
            {
                {
                    if (newDateTo != null)
                    {
                        ((DBModel.Task)entry).dateFinish = newDateTo.Value;
                    }
                }
            }

            context.SaveChanges();

            RestoreFilters(dateFrom, dateTo, allowNotes, allowMeetings, allowTasks);
        }

        public void OnPostDone(int? id, DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DiaryContext>());
            //DiaryContext context = new DiaryContext();

            var entries = (from e in context.Entries
                           where e.ID == id
                           select e);

            if (entries.Count() == 0)
            {
                message = "Запись с ID = " + id + " не найдена!"; //Что очень странно
                return;
            }

            Entry entry = entries.First();

            entry.Done = !entry.Done;

            context.SaveChanges();

            RestoreFilters(dateFrom, dateTo, allowNotes, allowMeetings, allowTasks);



        }
    }
}
