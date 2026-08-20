using Prism.Mvvm;

namespace Zitie.Desktop.ViewModels;

public sealed class CategoryChoiceViewModel : BindableBase
{
    private bool _isSelected;

    public CategoryChoiceViewModel(string name, bool isSelected)
    {
        Name = name;
        _isSelected = isSelected;
    }

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
