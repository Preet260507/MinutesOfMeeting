// using Microsoft.EntityFrameworkCore;
// using MOM_Project.Models; // This will now work because you created the folder!
//
// namespace MOM_Project.Data
// {
//     public class ApplicationDbContext : DbContext
//     {
//         public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
//         {
//         }
//
//         // This links your C# code to the Database Table
//         public DbSet<MeetingType> MeetingTypes { get; set; }
//         public DbSet<Department> Departments { get; set; }
//         public DbSet<Staff> StaffMembers { get; set; }
//         
//         public DbSet<MeetingMember> MeetingMembers { get; set; }
//         public DbSet<MeetingVenue> MeetingVenues { get; set; }
//         public DbSet<Meeting> Meetings { get; set; }
//     }
// }