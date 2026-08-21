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
            ["初一"] = 7,
            ["初二"] = 8,
            ["初三"] = 9,
            ["高一"] = 10,
            ["高二"] = 11,
            ["高三"] = 12,
            ["通用"] = 99
        };

    private static readonly IReadOnlyDictionary<string, int> EditionOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["统编版"] = 0,
            ["人教版"] = 1,
            ["北师大版"] = 2,
            ["外研版"] = 3,
            ["译林版"] = 4,
            ["通用版"] = 99
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
    private string _selectedEdition = All;
    private string _selectedGrade = All;
    private string _selectedSemester = All;
    private string _selectedUnit = All;
    private string _searchText = string.Empty;
    private TextEntry? _selectedEntry;
    private TextContentSearchResult? _selectedResult;
    private IReadOnlyList<string> _editions = [All];
    private IReadOnlyList<string> _grades = [All];
    private IReadOnlyList<string> _semesters = [All];
    private IReadOnlyList<string> _units = [All];
    private IReadOnlyList<TextEntry> _filteredEntries = [];
    private IReadOnlyList<TextContentSearchResult> _filteredItems = [];
    private IReadOnlyList<TextEntry> _recentEntries = [];
    private IReadOnlyList<TextContentSearchResult> _recentItems = [];

    public TextContentSelection(IReadOnlyList<TextEntry> entries)
    {
        _entries = entries;
        Subjects = BuildChoices(entries.Select(static entry => entry.Subject), SubjectOrder);
        Rebuild(resetEdition: true, resetGrade: true, resetSemester: true, resetUnit: true);
    }

    public event EventHandler<TextEntry>? EntrySelected;

    public IReadOnlyList<string> Subjects { get; }

    public IReadOnlyList<string> Editions
    {
        get => _editions;
        private set => SetChoices(ref _editions, value, nameof(Editions));
    }

    public IReadOnlyList<string> Grades
    {
        get => _grades;
        private set => SetChoices(ref _grades, value, nameof(Grades));
    }

    public IReadOnlyList<string> Semesters
    {
        get => _semesters;
        private set => SetChoices(ref _semesters, value, nameof(Semesters));
    }

    public IReadOnlyList<string> Units
    {
        get => _units;
        private set => SetChoices(ref _units, value, nameof(Units));
    }

    public IReadOnlyList<TextEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set
        {
            if (!SetProperty(ref _filteredEntries, value)) return;
            RaisePropertyChanged(nameof(ResultSummary));
            RaisePropertyChanged(nameof(ResultCountSummary));
            RaisePropertyChanged(nameof(FilterSummary));
        }
    }

    public string ResultSummary => $"{FilteredEntries.Count} 条内容";

    public string ResultCountSummary => $"{FilteredEntries.Count} / {_entries.Count} 篇";

    public string FilterSummary
    {
        get
        {
            var parts = new List<string>();
            AddSelected(parts, SelectedSubject);
            AddSelected(parts, SelectedEdition);
            AddSelected(parts, SelectedGrade);
            AddSelected(parts, SelectedSemester);
            AddSelected(parts, SelectedUnit);
            if (SearchText.Length > 0) parts.Add($"关键词「{SearchText}」");
            parts.Add($"命中 {FilteredEntries.Count} 篇");
            return string.Join(" · ", parts);
        }
    }

    public string SourceSummary
    {
        get
        {
            var entry = SelectedEntry;
            var parts = entry is null
                ? new[] { SelectedSubject, SelectedEdition }
                : new[] { entry.Subject, entry.Edition };
            var summary = string.Join(" · ", parts
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != All));
            return summary.Length > 0 ? summary : $"{_entries.Count} 条内容";
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (!SetProperty(ref _searchText, normalized)) return;
            Rebuild();
        }
    }

    public string SelectedSubject
    {
        get => _selectedSubject;
        set
        {
            var normalized = NormalizeChoice(value, Subjects);
            if (!SetProperty(ref _selectedSubject, normalized)) return;
            Rebuild(resetEdition: true, resetGrade: true, resetSemester: true, resetUnit: true);
            RaisePropertyChanged(nameof(SourceSummary));
        }
    }

    public IReadOnlyList<TextContentSearchResult> FilteredItems
    {
        get => _filteredItems;
        private set => SetProperty(ref _filteredItems, value);
    }

    public IReadOnlyList<TextContentSearchResult> RecentItems
    {
        get => _recentItems;
        private set
        {
            if (!SetProperty(ref _recentItems, value)) return;
            RaisePropertyChanged(nameof(HasRecentItems));
        }
    }

    public bool HasRecentItems => RecentItems.Count > 0;

    public string SelectedEdition
    {
        get => _selectedEdition;
        set
        {
            var normalized = NormalizeChoice(value, Editions);
            if (!SetProperty(ref _selectedEdition, normalized)) return;
            Rebuild(resetGrade: true, resetSemester: true, resetUnit: true);
            RaisePropertyChanged(nameof(SourceSummary));
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
        set => SelectEntry(value);
    }

    public TextContentSearchResult? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (value is null)
            {
                if (SetProperty(ref _selectedResult, null))
                    SelectedEntry = null;
                return;
            }

            SelectEntry(value.Entry, value);
        }
    }

    public void Reset()
    {
        var changed = _selectedSubject != All || _selectedEdition != All || _selectedGrade != All ||
                      _selectedSemester != All || _selectedUnit != All || _selectedEntry is not null ||
                      _searchText.Length > 0;
        _selectedSubject = All;
        _selectedEdition = All;
        _selectedGrade = All;
        _selectedSemester = All;
        _selectedUnit = All;
        _searchText = string.Empty;
        _selectedEntry = null;
        _selectedResult = null;
        Rebuild(resetEdition: true, resetGrade: true, resetSemester: true, resetUnit: true);
        if (!changed) return;

        RaisePropertyChanged(nameof(SelectedSubject));
        RaisePropertyChanged(nameof(SelectedEdition));
        RaisePropertyChanged(nameof(SelectedGrade));
        RaisePropertyChanged(nameof(SelectedSemester));
        RaisePropertyChanged(nameof(SelectedUnit));
        RaisePropertyChanged(nameof(SearchText));
        RaisePropertyChanged(nameof(SelectedEntry));
        RaisePropertyChanged(nameof(SelectedResult));
        RaisePropertyChanged(nameof(SourceSummary));
    }

    private void Rebuild(
        bool resetEdition = false,
        bool resetGrade = false,
        bool resetSemester = false,
        bool resetUnit = false)
    {
        var bySubject = Filter(_entries, static entry => entry.Subject, SelectedSubject);
        var editions = BuildChoices(bySubject.Select(static entry => entry.Edition), EditionOrder);
        var edition = resetEdition ? All : NormalizeChoice(_selectedEdition, editions);

        var byEdition = Filter(bySubject, static entry => entry.Edition, edition);
        var grades = BuildChoices(byEdition.Select(static entry => entry.Grade), GradeOrder);
        var grade = resetGrade ? All : NormalizeChoice(_selectedGrade, grades);

        var byGrade = Filter(byEdition, static entry => entry.Grade, grade);
        var semesters = BuildChoices(byGrade.Select(static entry => entry.Semester), SemesterOrder);
        var semester = resetSemester ? All : NormalizeChoice(_selectedSemester, semesters);

        var bySemester = Filter(byGrade, static entry => entry.Semester, semester);
        var units = BuildChoices(bySemester.Select(static entry => entry.Unit));
        var unit = resetUnit ? All : NormalizeChoice(_selectedUnit, units);

        SetSelectedField(ref _selectedEdition, edition, nameof(SelectedEdition));
        SetSelectedField(ref _selectedGrade, grade, nameof(SelectedGrade));
        SetSelectedField(ref _selectedSemester, semester, nameof(SelectedSemester));
        SetSelectedField(ref _selectedUnit, unit, nameof(SelectedUnit));
        Editions = editions;
        Grades = grades;
        Semesters = semesters;
        Units = units;

        var terms = ParseTerms(SearchText);
        FilteredEntries = Search(Filter(bySemester, static entry => entry.Unit, unit), terms)
            .OrderBy(static entry => entry.Unit, StringComparer.CurrentCulture)
            .ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
            .ToArray();
        FilteredItems = FilteredEntries
            .Select(entry => TextContentSearchResult.Create(entry, terms))
            .ToArray();
        RecentItems = _recentEntries
            .Select(entry => TextContentSearchResult.Create(entry, terms))
            .ToArray();

        if (_selectedEntry is null)
        {
            SetSelectedResult(null);
            return;
        }

        if (FilteredEntries.Contains(_selectedEntry))
        {
            SetSelectedResult(FilteredItems.FirstOrDefault(item => item.Entry == _selectedEntry));
            return;
        }

        _selectedEntry = null;
        SetSelectedResult(null);
        RaisePropertyChanged(nameof(SelectedEntry));
        RaisePropertyChanged(nameof(SourceSummary));
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

    private static IEnumerable<TextEntry> Search(IEnumerable<TextEntry> source, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0) return source;

        return source.Where(entry => terms.All(term => Matches(entry, term)));
    }

    private static IReadOnlyList<string> ParseTerms(string keyword)
    {
        return keyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static bool Matches(TextEntry entry, string term)
    {
        return Contains(entry.Subject, term) ||
               Contains(entry.Grade, term) ||
               Contains(entry.Semester, term) ||
               Contains(entry.Unit, term) ||
               Contains(entry.Title, term) ||
               Contains(entry.ResourceType, term) ||
               Contains(entry.Textbook, term) ||
               Contains(entry.Edition, term) ||
               Contains(entry.Dynasty, term) ||
               Contains(entry.Author, term) ||
               Contains(entry.Source, term) ||
               Contains(entry.Body, term);
    }

    private static bool Contains(string value, string term)
    {
        return value.Contains(term, StringComparison.CurrentCultureIgnoreCase);
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

    private void SetSelectedField(ref string field, string value, string propertyName)
    {
        if (field == value) return;
        field = value;
        RaisePropertyChanged(propertyName);
    }

    private void SetChoices(ref IReadOnlyList<string> field, IReadOnlyList<string> value, string propertyName)
    {
        if (field.SequenceEqual(value, StringComparer.Ordinal)) return;
        field = value;
        RaisePropertyChanged(propertyName);
    }

    private void SelectEntry(TextEntry? entry, TextContentSearchResult? result = null)
    {
        if (entry is not null && !_entries.Contains(entry)) return;
        if (!SetProperty(ref _selectedEntry, entry, nameof(SelectedEntry))) return;
        RaisePropertyChanged(nameof(SourceSummary));

        SetSelectedResult(result ?? FilteredItems.FirstOrDefault(item => item.Entry == entry));
        if (entry is null) return;

        AddRecentEntry(entry);
        EntrySelected?.Invoke(this, entry);
    }

    private void AddRecentEntry(TextEntry entry)
    {
        _recentEntries = _recentEntries
            .Where(item => !string.Equals(item.Identity, entry.Identity, StringComparison.Ordinal))
            .Prepend(entry)
            .Take(8)
            .ToArray();
        RecentItems = _recentEntries
            .Select(item => TextContentSearchResult.Create(item, ParseTerms(SearchText)))
            .ToArray();
    }

    private void SetSelectedResult(TextContentSearchResult? result)
    {
        SetProperty(ref _selectedResult, result, nameof(SelectedResult));
    }

    private static void AddSelected(ICollection<string> parts, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != All) parts.Add(value);
    }
}
