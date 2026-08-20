using Prism.Mvvm;

namespace Zitie.Desktop.Models;

/// <summary>封装内容库的级联筛选状态，避免编辑器 ViewModel 持有一组相互依赖的索引。</summary>
public sealed class TextContentSelection : BindableBase
{
    public const string All = "全部";

    private static readonly IReadOnlyDictionary<string, int> SubjectOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["语文"] = 0,
            ["英语"] = 1,
            ["名言警句"] = 2
        };

    private static readonly IReadOnlyDictionary<string, int> GradeOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["一年级"] = 1,
            ["二年级"] = 2,
            ["三年级"] = 3,
            ["四年级"] = 4,
            ["五年级"] = 5,
            ["六年级"] = 6,
            ["通用"] = 99
        };

    private static readonly IReadOnlyDictionary<string, int> SemesterOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["上册"] = 0,
            ["下册"] = 1,
            ["课外"] = 2
        };

    private readonly IReadOnlyList<TextEntry> _entries;
    private string _selectedSubject = All;
    private string _selectedGrade = All;
    private string _selectedSemester = All;
    private string _selectedUnit = All;
    private TextEntry? _selectedEntry;
    private IReadOnlyList<string> _grades = [All];
    private IReadOnlyList<string> _semesters = [All];
    private IReadOnlyList<string> _units = [All];
    private IReadOnlyList<TextEntry> _filteredEntries = [];

    public TextContentSelection(IReadOnlyList<TextEntry> entries)
    {
        _entries = entries;
        Subjects = BuildChoices(entries.Select(static entry => entry.Subject), SubjectOrder);
        Rebuild(resetGrade: true, resetSemester: true, resetUnit: true);
    }

    public event EventHandler<TextEntry>? EntrySelected;

    public IReadOnlyList<string> Subjects { get; }

    public IReadOnlyList<string> Grades
    {
        get => _grades;
        private set => SetProperty(ref _grades, value);
    }

    public IReadOnlyList<string> Semesters
    {
        get => _semesters;
        private set => SetProperty(ref _semesters, value);
    }

    public IReadOnlyList<string> Units
    {
        get => _units;
        private set => SetProperty(ref _units, value);
    }

    public IReadOnlyList<TextEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set
        {
            if (!SetProperty(ref _filteredEntries, value)) return;
            RaisePropertyChanged(nameof(ResultSummary));
        }
    }

    public string ResultSummary => $"{FilteredEntries.Count} 条内容";

    public string SelectedSubject
    {
        get => _selectedSubject;
        set
        {
            var normalized = NormalizeChoice(value, Subjects);
            if (!SetProperty(ref _selectedSubject, normalized)) return;
            Rebuild(resetGrade: true, resetSemester: true, resetUnit: true);
        }
    }

    public string SelectedGrade
    {
        get => _selectedGrade;
        set
        {
            var normalized = NormalizeChoice(value, Grades);
            if (!SetProperty(ref _selectedGrade, normalized)) return;
            Rebuild(resetSemester: true, resetUnit: true);
        }
    }

    public string SelectedSemester
    {
        get => _selectedSemester;
        set
        {
            var normalized = NormalizeChoice(value, Semesters);
            if (!SetProperty(ref _selectedSemester, normalized)) return;
            Rebuild(resetUnit: true);
        }
    }

    public string SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            var normalized = NormalizeChoice(value, Units);
            if (!SetProperty(ref _selectedUnit, normalized)) return;
            Rebuild();
        }
    }

    public TextEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (value is not null && !FilteredEntries.Contains(value)) return;
            if (!SetProperty(ref _selectedEntry, value) || value is null) return;
            EntrySelected?.Invoke(this, value);
        }
    }

    public void Reset()
    {
        var changed = _selectedSubject != All || _selectedGrade != All ||
                      _selectedSemester != All || _selectedUnit != All || _selectedEntry is not null;
        _selectedSubject = All;
        _selectedGrade = All;
        _selectedSemester = All;
        _selectedUnit = All;
        _selectedEntry = null;
        Rebuild(resetGrade: true, resetSemester: true, resetUnit: true);
        if (!changed) return;

        RaisePropertyChanged(nameof(SelectedSubject));
        RaisePropertyChanged(nameof(SelectedGrade));
        RaisePropertyChanged(nameof(SelectedSemester));
        RaisePropertyChanged(nameof(SelectedUnit));
        RaisePropertyChanged(nameof(SelectedEntry));
    }

    private void Rebuild(bool resetGrade = false, bool resetSemester = false, bool resetUnit = false)
    {
        var bySubject = Filter(_entries, static entry => entry.Subject, SelectedSubject);
        Grades = BuildChoices(bySubject.Select(static entry => entry.Grade), GradeOrder);
        if (resetGrade) SetSelectedField(ref _selectedGrade, All, nameof(SelectedGrade));
        else NormalizeSelectedField(ref _selectedGrade, Grades, nameof(SelectedGrade));

        var byGrade = Filter(bySubject, static entry => entry.Grade, SelectedGrade);
        Semesters = BuildChoices(byGrade.Select(static entry => entry.Semester), SemesterOrder);
        if (resetSemester) SetSelectedField(ref _selectedSemester, All, nameof(SelectedSemester));
        else NormalizeSelectedField(ref _selectedSemester, Semesters, nameof(SelectedSemester));

        var bySemester = Filter(byGrade, static entry => entry.Semester, SelectedSemester);
        Units = BuildChoices(bySemester.Select(static entry => entry.Unit));
        if (resetUnit) SetSelectedField(ref _selectedUnit, All, nameof(SelectedUnit));
        else NormalizeSelectedField(ref _selectedUnit, Units, nameof(SelectedUnit));

        FilteredEntries = Filter(bySemester, static entry => entry.Unit, SelectedUnit)
            .OrderBy(static entry => entry.Unit, StringComparer.CurrentCulture)
            .ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
            .ToArray();

        if (_selectedEntry is null || FilteredEntries.Contains(_selectedEntry)) return;
        _selectedEntry = null;
        RaisePropertyChanged(nameof(SelectedEntry));
    }

    private static IReadOnlyList<TextEntry> Filter(
        IEnumerable<TextEntry> source,
        Func<TextEntry, string> selector,
        string choice)
    {
        return choice == All
            ? source.ToArray()
            : source.Where(entry => string.Equals(selector(entry), choice, StringComparison.Ordinal)).ToArray();
    }

    private static IReadOnlyList<string> BuildChoices(
        IEnumerable<string> values,
        IReadOnlyDictionary<string, int>? preferredOrder = null)
    {
        var ordered = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => preferredOrder?.GetValueOrDefault(value, int.MaxValue) ?? int.MaxValue)
            .ThenBy(static value => value, StringComparer.CurrentCulture)
            .Prepend(All)
            .ToArray();
        return ordered;
    }

    private static string NormalizeChoice(string? value, IReadOnlyList<string> choices)
    {
        return !string.IsNullOrWhiteSpace(value) && choices.Contains(value, StringComparer.Ordinal) ? value : All;
    }

    private void NormalizeSelectedField(ref string field, IReadOnlyList<string> choices, string propertyName)
    {
        SetSelectedField(ref field, NormalizeChoice(field, choices), propertyName);
    }

    private void SetSelectedField(ref string field, string value, string propertyName)
    {
        if (field == value) return;
        field = value;
        RaisePropertyChanged(propertyName);
    }
}
