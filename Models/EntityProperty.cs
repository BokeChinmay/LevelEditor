using CommunityToolkit.Mvvm.ComponentModel;

namespace LevelEditor.Models;

public partial class EntityProperty : ObservableObject {
    [ObservableProperty] private string _key = "";
    [ObservableProperty] private string _value = "";

    public EntityProperty(string key, string value) {
        _key = key;
        _value = value;
    }
}