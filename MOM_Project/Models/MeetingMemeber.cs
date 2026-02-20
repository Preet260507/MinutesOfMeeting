using MOM_Project.Models;

namespace MOM_Project.ViewModels
{
    public class MeetingMemberVM
    {
        // Context: Which meeting are we managing?
        public int MeetingID { get; set; }
        public string MeetingTitle { get; set; }
        public DateTime MeetingDate { get; set; }

        // Data: The list of people currently in the meeting
        public List<Staff> CurrentMembers { get; set; } = new List<Staff>();

        // Form: IDs selected in the checkboxes
        public List<int> SelectedStaffIds { get; set; } = new List<int>();
    }
}