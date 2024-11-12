namespace PropertyManagement.Models
{
    public class Appointment
    {
        public int Id { get; set; }  
        public DateTime AppointmentDate { get; set; }  

        public string ClientId { get; set; } 
        public ApplicationUser Client { get; set; } 

        public int PropertyId { get; set; } 
        public Property Property { get; set; }  
    }

}
