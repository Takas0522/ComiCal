namespace ComiCal.Application.Dtos;

public record CalendarDayDto(string Date, IReadOnlyList<VolumeDto> Volumes);

public record CalendarResult(
    IReadOnlyList<CalendarDayDto> Days,
    IReadOnlyList<VolumeDto> UndatedVolumes);
