using System;

namespace Database.MongoDB.Migration.Document
{
    internal class MigrationDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime CreatedDate { get; set; }
    }   
}