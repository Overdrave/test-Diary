using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace DBModel
{
    public class Entry
    {
        public int ID { get; set; }
        //public int Type { get; set; } //0 - памятка, 1 - встреча, 2 - дело
        public bool Done { get; set; }
        public string Subject { get; set; }
        public string Details { get; set; }
        public DateTime dateStart { get; set; }
        
    }

    public class Meeting : Entry
    {
        public DateTime dateFinish { get; set; } //Используется везде, кроме памятки
        public string Place { get; set; } //Используется только в типе Встреча
    }
    public class Task : Entry
    {
        public DateTime dateFinish { get; set; } //Используется везде, кроме памятки
    }

    public class Note : Entry
    {

    }

    public class DiaryContext : DbContext
    {
        // Имя будущей базы данных можно указать через
        // вызов конструктора базового класса
        public DiaryContext() : base("Diary")
        {
        }

        // Отражение таблиц базы данных на свойства с типом DbSet
        public DbSet<Entry> Entries { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<Note> Notes { get; set; }

    }
}
