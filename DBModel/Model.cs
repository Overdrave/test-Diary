using System;
using System.Data.Entity;

namespace DBModel
{
    public class Entry
    {
        public int ID { get; set; }
        public int Type { get; set; } //0 - памятка, 1 - встреча, 2 - дело
        public bool Done { get; set; }
        public string Subject { get; set; }
        public string Details { get; set; }
        public DateTime dateStart { get; set; }
        public DateTime dateFinish { get; set; } //Используется везде, кроме памятки
        public string Place { get; set; } //Используется только в типе Встреча
    }

    public class DiaryContext : DbContext
    {
        // Имя будущей базы данных можно указать через
        // вызов конструктора базового класса
        public DiaryContext() : base("Diary")
        { }

        // Отражение таблиц базы данных на свойства с типом DbSet
        public DbSet<Entry> Entries { get; set; }
    }
}
