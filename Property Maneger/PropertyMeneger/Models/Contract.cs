namespace PropertyManagement.Models
{
    public class Contract
    {
        public int Id { get; set; } 
        public DateTime StartDate { get; set; }  
        public DateTime EndDate { get; set; }  
        public decimal Amount { get; set; }  

        public int PropertyId { get; set; } 
        public Property Property { get; set; }  
        public string CustomerId { get; set; } 
        public ApplicationUser Customer { get; set; }  

        public string OwnerId { get; set; }  
        public ApplicationUser Owner { get; set; }  


}
