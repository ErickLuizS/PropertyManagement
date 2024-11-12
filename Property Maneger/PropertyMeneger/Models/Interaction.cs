namespace PropertyManagement.Models
{
    public class Interaction
    {
        public int Id { get; set; }  
        public string CustomerId { get; set; } 
        public ApplicationUser Customer { get; set; } 

        public int PropertyId { get; set; }  
        public Property Property { get; set; }  

        public DateTime InteractionDate { get; set; } 
        public string InteractionType { get; set; }  
        public double InteractionValue { get; set; }  
    }
}
