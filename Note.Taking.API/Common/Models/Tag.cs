namespace Note.Taking.API.Common.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public List<NoteTag> NoteTags { get; set; } 
    }
}
