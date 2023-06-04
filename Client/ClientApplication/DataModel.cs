using System.ComponentModel;

namespace Mapster.ClientApplication;

// Implementation of a dynamic two-way bindable data model
// (https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged?view=net-6.0)
public class DataModel : INotifyPropertyChanged
{
    private string _data;

    public DataModel(string initialValue = "Default value")
    {
        _data = initialValue;
    }

    // Set up the property to invoke the event when the setter changes the value
    public string Data
    {
        get => _data;
        set
        {
            if (_data != value)
            {
                _data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}