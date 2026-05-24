# Глобальний Прогрес Обробки Відео - Документація

## Огляд

Реалізована система глобального прогресу для обробки адаптивного бітрейту (ABR) HLS потоків. Прогрес враховує всі якості відео разом замість окремого прогресу для кожної якості.

## Проблема, яка була вирішена

### До оновлення:
```
360p:  12% → 25% → 50% → 88% (завершено) ✓
480p:  12% → 24% → 57% → 88% (завершено) ✓   ← Прогрес знову скинувся на 12%!
720p:  12% → ...
1080p: 12% → ...
```

Проблема: При переході до наступної якості прогрес скидувався назад, що призводило до розривів і неправильних оновлень у інтерфейсі.

### Після оновлення:
```
10% → 15% → 20% → 25% → 28% → 32% → 35% → 39% 
→ 42% → 46% → 50% → 53% → 55% → 59% → 62% → 66%
→ 70% → 73% → 75% → 78% → 80% → 82% → 85% → 88% → 90%
```

Прогрес монотонно зростає через всі якості без скидування назад!

## Архітектура

### 1. VideoProcessor.cs

#### ConvertToHlsAsync()
```csharp
public async Task<string> ConvertToHlsAsync(
    string inputFilePath,
    string outputBaseDir,
    Func<int, Task>? onProgress,
    CancellationToken ct)
```

**Логіка:**
- Отримує список усіх якостей (максимум 4: 360p, 480p, 720p, 1080p)
- Ініціалізує лічильники: `totalQualities`, `completedQualities = 0`
- Цикл обробки кожної якості:
  - Передає поточні лічильники в `ProcessQualityAsync()`
  - Після завершення якості інкрементує `completedQualities++`

#### ProcessQualityAsync()
```csharp
private async Task ProcessQualityAsync(
    string inputFilePath,
    string outputBaseDir,
    HlsQuality quality,
    TimeSpan? totalDuration,
    int completedQualities,      // ← Кількість завершених якостей
    int totalQualities,          // ← Загальна кількість якостей
    Func<int, Task>? onProgress,
    CancellationToken ct)
```

**Логіка:**
1. Читає stderr від FFmpeg
2. Парсить `time=HH:MM:SS.ff` з кожного рядка
3. Рахує локальний прогрес якості: `(time / totalDuration) * 100`
4. **Рахує глобальний прогрес:**
   ```csharp
   overallProgressRaw = 
       (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;

   overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
   overallProgress = Math.Clamp(overallProgress, 10, 90);
   ```
5. Викликає callback: `await onProgress(overallProgress)`

### 2. ProcessVideoCommandHandler.cs

**Callback реалізація:**
```csharp
await _videoProcessor.ConvertToHlsAsync(
    tempInput,
    tempOutputDir,
    async progress =>
    {
        video.Progress = progress;
        await _dbContext.SaveChangesAsync(ct);
        await NotifyHub(fileId, "Processing", progress);
    },
    ct);
```

**Результат:**
- Кожне оновлення прогресу зберігається в БД
- SignalR оновлення надсилаються клієнтам в реальному часі

## Формула Розрахунку

### Крок 1: Локальний прогрес якості
```
currentQualityProgress = (currentTime / totalDuration) * 100
```

Приклад:
- totalDuration = 100 сек
- currentTime = 25 сек
- currentQualityProgress = (25 / 100) * 100 = 25%

### Крок 2: Глобальний прогрес без масштабування
```
overallProgressRaw = 
    (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100
```

Приклад (4 якості):
- completedQualities = 1 (360p завершено)
- currentQualityProgress = 50% (480p на 50%)
- overallProgressRaw = (1 + 0.5) / 4 * 100 = 37.5%

### Крок 3: Масштабування до 10-90%
```
overallProgress = 10 + Math.Round(overallProgressRaw * 0.8)
overallProgress = Math.Clamp(overallProgress, 10, 90)
```

Приклад:
- overallProgressRaw = 37.5%
- overallProgress = 10 + (37.5 * 0.8) = 10 + 30 = 40%

## Енергонезалежна послідовність прогресу

### Для 4 якостей (360p, 480p, 720p, 1080p):

| Стан | completedQualities | currentQuality | progress | Overall |
|------|-------------------|----------------|----------|---------|
| Старт | 0 | 0% | 0% | 10% |
| | 0 | 25% | 6.25% | 15% |
| | 0 | 50% | 12.5% | 20% |
| | 0 | 75% | 18.75% | 25% |
| | 0 | 100% | 25% | 30% |
| 360p готово | 1 | 0% | 25% | 30% |
| | 1 | 25% | 31.25% | 35% |
| | 1 | 50% | 37.5% | 40% |
| | 1 | 75% | 43.75% | 45% |
| 480p готово | 2 | 0% | 50% | 50% |
| | 2 | 25% | 56.25% | 55% |
| | 2 | 50% | 62.5% | 60% |
| | 2 | 75% | 68.75% | 65% |
| 720p готово | 3 | 0% | 75% | 70% |
| | 3 | 25% | 81.25% | 75% |
| | 3 | 50% | 87.5% | 80% |
| | 3 | 75% | 93.75% | 85% |
| 1080p готово | 4 | 100% | 100% | 90% |

## Переваги реалізації

1. ✅ **Монотонна прогресія** - прогрес ніколи не зменшується
2. ✅ **Лінійна сходження** - користувач бачить постійне збільшення
3. ✅ **Глобальна перспектива** - прогрес показує прогрес всього процесу
4. ✅ **Без розривів** - плавна прогресія між якостями
5. ✅ **Реальний час** - оновлення щомайже 500ms
6. ✅ **SignalR інтеграція** - миттєві оновлення в інтерфейсі

## Логування

Кожне оновлення прогресу логується:
```
Progress [360p] Current=25% Overall=15%
Progress [360p] Current=50% Overall=20%
Progress [480p] Current=25% Overall=35%
```

## Тестування

Див. `PROGRESS_CALCULATION_TESTS.md` для детальних тестів з прикладами.

## Сумісність

- ✅ .NET 8
- ✅ .NET 10
- ✅ ASP.NET Core
- ✅ SignalR
- ✅ Entity Framework Core
