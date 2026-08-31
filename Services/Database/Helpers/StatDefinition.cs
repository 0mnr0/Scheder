namespace Scheder.Services.Database.Helpers;

public class StatDefinition {

    public class ActionTypes {
        // ReSharper disable once InconsistentNaming
        public readonly string DIRECT_ASK = "COMMAND_ASK";
        // ReSharper disable once InconsistentNaming
        public readonly string DIRECT_CONTEXT_ASK = "DIRECT_CONTEXT_ASK";
        // ReSharper disable once InconsistentNaming
        public readonly string CALLBACK_UPDATE = "CALLBACK_UPDATE";
        // ReSharper disable once InconsistentNaming
        public readonly string FOREGROUND_ASK = "FOREGROUND_ASK";
    }
}