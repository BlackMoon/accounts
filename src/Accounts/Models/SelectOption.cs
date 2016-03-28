namespace accounts.Models
{
    /// <summary>
    /// option-элемент html-select'a
    /// </summary>
    public class SelectOption
    {
        public string Text { get; set; }

        public string Value { get; set; }

        public SelectOption(string value) : this(value, value)
        {
        }

        public SelectOption(string value, string text)
        {
            Text = text;
            Value = value;
        }
    }
}
