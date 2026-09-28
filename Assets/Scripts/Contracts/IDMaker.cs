using System;

namespace Global
{
    public class IDMaker
    {
        public static string GetUserID()
        {
            return Guid.NewGuid().ToString();
        }

        private static int curUseID = 0;
    
        public static int GetCardUseInputID()
        {
            int id = curUseID;
            curUseID++;
            return id;
        }
    }
}
