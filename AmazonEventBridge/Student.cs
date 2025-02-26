using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonEventBridge
{
    public class Student
    {
        public string StudentID { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string DateOfBirth { get; set; }
    }

    public class StudentRegisteredEvent
    {
        public string Version { get; set; }
        public string Id { get; set; }
        public string DetailType { get; set; }
        public string Source { get; set; }
        public string Account { get; set; }
        public DateTime Time { get; set; }
        public string Region { get; set; }
        public List<object> Resources { get; set; }
        public Detail Detail { get; set; }
    }

    public class Detail
    {
        public string StudentID { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

}