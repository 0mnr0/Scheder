namespace Scheder.Services.iWillBeLate;

public class ApiCodesTranslator {
    
    public static DirectTimeMissApi.MissedTimeSendResult UpdateDescription(DirectTimeMissApi.MissedTimeSendResult? serverResponse) {

        if (serverResponse == null) {
            return new DirectTimeMissApi.MissedTimeSendResult {
                Action = -1,
                ShowText = "Что-то пошло не так :("
            };
        }
        
        if (serverResponse.Success) {
            // update / create
            serverResponse.ShowText = serverResponse.Action == 1 ? "Изменения сохранены!" : "Отметка удалена!";
        }
        else {
            serverResponse.ShowText = serverResponse.Action == 1 ? "Не удалось сохранить изменения :(" : "Не удалось удалить отметку :(";
        }
        
        return serverResponse;
        
    }
    
}