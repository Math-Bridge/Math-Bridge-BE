public class AddChildRequest
{
    public string FullName { get; set; }
    public Guid SchoolId { get; set; }
    public Guid? CenterId { get; set; }
    public string Grade { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}