namespace PropertyManagement.Models
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.VisualBasic;

    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }


        public string UserType { get; set; } 

        public List<Property> Properties { get; set; }
        public List<Interaction> Interactions { get; set; }
        public List<Appointment> Appointments { get; set; } 



    }
}