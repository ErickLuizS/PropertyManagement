namespace PropertyManagement.Models
{
    public class Location
    {
        public int Id { get; set; }  
        public string Address { get; set; }  

        public string City { get; set; }
        public double Latitude { get; set; }  
        public double Longitude { get; set; }  

       
        public int PropertyId { get; set; }  
        public Property Property { get; set; } 
    }

}
