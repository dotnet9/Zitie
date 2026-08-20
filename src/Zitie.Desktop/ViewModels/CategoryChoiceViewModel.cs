using Prism.Commands;
using Prism.Mvvm;

namespace Zitie.Desktop.ViewModels;

public sealed class CategoryChoiceViewModel : BindableBase
{
    private bool _isSelected;

    public CategoryChoiceViewModel(string name, bool isSelected, Action<string> select)
    {
        Name = name;
        _isSelected = isSelected;
        SelectCommand = new DelegateCommand(() => select(Name));
    }

    public string Name { get; }

    public DelegateCommand SelectCommand { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
