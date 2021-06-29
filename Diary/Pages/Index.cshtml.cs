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

        public IEnumerable<DBModel.Entry> entries;
        public IEnumerable<DBModel.Meeting> meetings;
        public IEnumerable<DBModel.Task> tasks;
        public IEnumerable<DBModel.Note> notes;





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


        public void OnGet(DateTime dateFrom, DateTime dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
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
            DiaryContext context = new DiaryContext();


            //...но сначала проверяем, есть ли она
            if (!context.Entries.Any())
            {
                //Если её нет, то ничего не делаем
            }
            else
            {
                //Иначе же применяем фильтры и достаём
                notes = from n in context.Notes
                        where this.allowNotes && n.dateStart <= dateTo && n.dateStart >= dateFrom
                        select n;
                meetings = from m in context.Meetings
                           where this.allowMeetings && m.dateStart <= dateTo && m.dateFinish >= dateFrom
                           select m;
                tasks = from t in context.Tasks
                        where this.allowTasks && t.dateStart <= dateTo && t.dateFinish >= dateFrom
                        select t;
                entries = notes.Concat(meetings as IEnumerable<Entry>).Concat(tasks);
                ;
                //join m in context.Meetings on e.ID equals m.ID
                //join t in context.Tasks on e.ID equals t.ID
                //join n in context.Notes on e.ID equals n.ID
                //          where (e.dateStart <= dateTo) && (/*(m.dateFinish >= dateFrom) || (t.dateFinish >= dateFrom) ||*/true || (e is Note && e.dateStart>=dateFrom))
                //          &&
                //          (
                //          (allowMeetings && (e is Meeting))
                //          || (allowTasks && (e is DBModel.Task))
                //          || (allowNotes && (e is Note))
                //          )
                //          select e


                ; //RangeCollide(e.dateStart,e.dateFinish,this.dateFrom, this.dateTo) 
            }

        }

        public void OnPost(int? entryType, string? entrySubject, string? entryDetails, DateTime? entryDateFrom, DateTime? entryDateTo, string? entryPlace)
        {
            DiaryContext context = new DiaryContext();
            if (entryDateFrom == null)
            {
                StatusCode(400);
                return;
            }

            if (entryType == 1)
            {
                if (entryDateTo == null)
                {
                    StatusCode(400);
                    return;
                }
                Meeting entry = new Meeting();
                entry.Done = false;
                entry.Subject = entrySubject;
                entry.Details = entryDetails;
                entry.dateStart = entryDateFrom.Value;
                entry.dateFinish = (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value;
                entry.Place = entryPlace;
                context.Meetings.Add(entry);
            }
            else if (entryType == 2)
            {
                DBModel.Task entry = new DBModel.Task();
                entry.Done = true; //Временно
                entry.Subject = entrySubject;
                entry.Details = entryDetails;
                entry.dateStart = entryDateFrom.Value;
                entry.dateFinish = (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value;
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

            OnGet(entryDateFrom.Value.Date,
                (entryDateTo != null) ? entryDateTo.Value : entryDateFrom.Value.Date.AddDays(1),
                (this.allowNotes) ? 1 : 0,
                (this.allowMeetings) ? 1 : 0,
                this.allowTasks ? 1 : 0);
        }

        public void OnPostDelete(int? id, DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            DiaryContext context = new DiaryContext();

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

            //Редирект обратно
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

        public void OnPostEdit(int? id, string? newSubject, string? newDetails, DateTime? newDateFrom, DateTime? newDateTo, string? newPlace, /*А теперь старые установки фильтров*/ DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            DiaryContext context = new DiaryContext();

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

        public void OnPostDone(int? id, DateTime? dateFrom, DateTime? dateTo, int? allowNotes, int? allowMeetings, int? allowTasks)
        {
            DiaryContext context = new DiaryContext();

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
    }
}
