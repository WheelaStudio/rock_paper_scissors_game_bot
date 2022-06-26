using System.ComponentModel;
using System.Reflection;
namespace rock_paper_scissors_game_bot.Utilities
{
    public class Configuration
    {
        class FieldValueChangedHandler
        {
            public string fieldName { get; private set; }
            private Action handleAction;
            public FieldValueChangedHandler(string fieldName, Action handleAction)
            {
                this.fieldName = fieldName;
                this.handleAction = handleAction;
            }
            public void Invoke()
            {
                handleAction?.Invoke();
            }
        }
        private readonly string[] variables = { "BOT_TOKEN", "ADMIN_ID", "SHOW_DEBUG_INFO" };
        #region Config fields
        private string? BOT_TOKEN;
        private long? ADMIN_ID;
        private bool? SHOW_DEBUG_INFO;
        #endregion
        private static Configuration? _instance;
        private Database dataBase;
        private List<FieldValueChangedHandler> fieldValueChangedHandlers = new();
        public Configuration()
        {
            dataBase = Database.Instance;
            foreach (var variable in variables)
                _ = GetConfigValue(variable);
        }
        public static Configuration Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new();
                return _instance;
            }
        }
        public string Bot_token => BOT_TOKEN!;
        public long Admin_id => (long)ADMIN_ID!;
        public bool Show_Debug_Info => (bool)SHOW_DEBUG_INFO!;
        public void AddFieldValueChangedHandler(object field, Action handleAction)
        {
            var fieldName = nameof(field).ToUpper();
            if (!variables.Contains(fieldName))
                throw new ArgumentException("This field does not exist!");
            fieldValueChangedHandlers.Add(new FieldValueChangedHandler(fieldName, handleAction));
        }
        private string GetConfigValue(string variable)
        {
            var fieldValue = typeof(Configuration).GetField(variable,
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this);
            if (fieldValue != null)
                return fieldValue.ToString()!;
            var command = dataBase.CreateCommand($"SELECT * FROM config WHERE VARIABLE = '{variable}'");
            using (var reader = command.ExecuteReader())
            {
                var value = reader.Read() ? reader.GetString(1) : "";
                SetFieldValue(variable, value);
                return value!;
            }
        }
        public void SetConfigValue(string variable, string value)
        {
            SetFieldValue(variable, value);
            var command = dataBase.CreateCommand($"UPDATE config SET VALUE = '{value}' WHERE VARIABLE = '{variable}'");
            _ = command.ExecuteNonQuery();
            foreach (var handler in fieldValueChangedHandlers)
            {
                if (handler.fieldName == variable)
                    handler.Invoke();
            }
        }
        private void SetFieldValue(string variable, string value)
        {
            var field = typeof(Configuration).GetField(variable, BindingFlags.NonPublic | BindingFlags.Instance)!;
            field.SetValue(this, TypeDescriptor.GetConverter(field.FieldType).ConvertFromString(value));
        }
        public Dictionary<string, string> Config
        {
            get
            {
                var dictionary = new Dictionary<string, string>();
                foreach (var variable in variables)
                    dictionary.Add(variable, GetConfigValue(variable));
                return dictionary;
            }
        }
    }
}


