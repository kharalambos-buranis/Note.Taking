namespace Note.Taking.API.Common.Models
{
    public class Note
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } 

        public string Title { get; set; }
        public string Content { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<NoteTag> NoteTags { get; set; } = new();
    }
}
