using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.DTOs
{
    public class Response
    {
        public string Message { get; set; }  
        public Dictionary<string, object> Data { get; set; } = null;
        public bool IsSuccess { get; set; }
    }
}