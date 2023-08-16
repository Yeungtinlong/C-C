using System.Collections.Generic;

namespace Danny.SaveSystem
{
    public class Save
    {
        public Dictionary<string, List<SerializedUnit>> Units = new Dictionary<string, List<SerializedUnit>>();
    }
}