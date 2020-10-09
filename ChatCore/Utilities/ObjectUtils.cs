namespace ChatCore.Utilities
{
    public static class ObjectUtils
    {
        public static object GetFieldValue(this object obj, string fieldName)
        {
            return obj.GetType().GetField(fieldName).GetValue(obj);
        }
    }
}
