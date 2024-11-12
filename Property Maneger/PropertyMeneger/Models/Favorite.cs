namespace PropertyManagement.Models
{
    public class Favorite
    {
        public int Id { get; set; } 

      
        public string ClientId { get; set; } 
        public ApplicationUser Client { get; set; } 

        public int PropertyId { get; set; } 
        public Property Property { get; set; } 

}
