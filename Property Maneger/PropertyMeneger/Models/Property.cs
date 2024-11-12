using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models
{
    public class Property
    {
        
        public int Id { get; set; } 
        [Required(ErrorMessage = "The property title is required.")]
        [StringLength(100, ErrorMessage = "The property title cannot exceed 100 characters.")]                             // a
        public string Title { get; set; }  

        [Required(ErrorMessage = "The description is required.")]
        [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
        public string Description { get; set; } 

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }  

        public string Address { get; set; }  
        public int Bedrooms { get; set; }  
        public int Bathrooms { get; set; } 
        public double Area { get; set; }  
        public DateTime CreatedDate { get; set; }  

        
        public string OwnerId { get; set; }  
        public ApplicationUser Owner { get; set; }  

        [Required(ErrorMessage = "The location is required.")]
        public Location Location { get; set; }  

        public List<Appointment> Appointments { get; set; }  
        public List<Interaction> Interactions { get; set; }  

        public ICollection<PropertyImage> Images { get; set; } 

    }
}

