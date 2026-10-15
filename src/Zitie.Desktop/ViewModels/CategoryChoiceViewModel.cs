using Prism.Commands;
using Prism.Mvvm;

namespace Zitie.Desktop.ViewModels;

public sealed class CategoryChoiceViewModel : LocalizedBindableBase
{
    private bool _isSelected;
    private readonly string _value;

    public CategoryChoiceViewModel(string name, bool isSelected, Action<string> select)
    {
        _value = name;
        _isSelected = isSelected;
        SelectCommand = new DelegateCommand(() => select(_value));
    }

    public string Name => CatalogText.DisplayCategory(_value);

    public DelegateCommand SelectCommand { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
