using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Drawing;
using DBModel;

namespace Diary.Pages
{
    public class IndexModel : PageModel
    {
        public DateTime timeNow = DateTime.Now;
        public DateTime dateFrom;
        public DateTime dateTo;

        public IEnumerable<DBModel.Entry> entries;

        

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

        public void OnGet(DateTime dateFrom, DateTime dateTo, int typeFilter = -1) //typeFilter == -1 == Any
        {
            if (dateFrom == null || dateTo == null || dateFrom.Year<2010 || dateTo.Year<2010)
            {
                this.dateFrom = timeNow.Date;
                this.dateTo = this.dateFrom.AddDays(1);
            }
            else
            {
                this.dateFrom = dateFrom;
                this.dateTo = dateTo;
            }
            //На всякий случай проверяем typeFilter
            if (typeFilter > 2 || typeFilter < -1)
                typeFilter = -1;

            //Достаём информацию из базы данных
            DiaryContext context = new DiaryContext();

            //...но сначала проверяем, есть ли она
            if (!context.Entries.Any())
            {
                //Если её нет, то ничего не делаем
            }
            else
            {
                //Иначе же применяем фильтры и достаём
                entries = from e in context.Entries
                          where e.dateStart<=this.dateTo && e.dateFinish>=this.dateFrom
                          && (typeFilter==-1)?true:e.Type==typeFilter
                          select e; //RangeCollide(e.dateStart,e.dateFinish,this.dateFrom, this.dateTo) 
            }

        }

        public void OnPost(int? entryType, string? entrySubject, string? entryDetails, DateTime entryDateFrom, DateTime entryDateTo, string? entryPlace)
        {
            Entry entry = new Entry();
            entry.Done = false;
            entry.Type = (int) entryType;
            entry.Subject = entrySubject;
            entry.Details = entryDetails;
            entry.dateStart = entryDateFrom;
            entry.dateFinish = (entryType==0) ? entryDateFrom : entryDateTo;
            entry.Place = (entryType==1) ? entryPlace : null;

                // Создать объект контекста
                DiaryContext context = new DiaryContext();

                // Вставить данные в таблицу Customers с помощью LINQ
                context.Entries.Add(entry);

                // Сохранить изменения в БД
                context.SaveChanges();

            OnGet(entryDateFrom.Date, entryDateFrom.Date.AddDays(1));
        }
    }
}
