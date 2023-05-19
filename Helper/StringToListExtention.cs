using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TaskManager.API.Helper
{
    public static class StringToListExtention
    {
        public static List<int> ConvertStringToList(this string? taskOrder){
            if (taskOrder == "") return null;
            var listIdTaskItem = JsonConvert.DeserializeObject<List<int>>(taskOrder);
            
            return listIdTaskItem;
        }
    }
}