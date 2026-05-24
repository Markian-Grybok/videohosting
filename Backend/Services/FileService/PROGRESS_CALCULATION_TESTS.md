/**
 * UNIT TEST: Global Progress Calculation
 * 
 * Тестування глобального прогресу для 4 якостей
 * 
 * Сценарій: видео тривалістю 100 секунд
 * Якості: 360p, 480p, 720p, 1080p
 * totalQualities = 4
 */

// Формула розрахунку:
// overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100
// overallProgress = 10 + Math.Round(overallProgressRaw * 0.8)
// overallProgress = Math.Clamp(overallProgress, 10, 90)

// ============================================================
// ТЕСТ 1: Обробка 360p на 0%
// ============================================================
int completedQualities = 0;
int totalQualities = 4;
double currentQualityProgress = 0;

double overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (0 + 0) / 4 * 100 = 0%

int overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (0 * 0.8) = 10%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 10%
// Результат: ✓ 10%

// ============================================================
// ТЕСТ 2: Обробка 360p на 25%
// ============================================================
completedQualities = 0;
currentQualityProgress = 25;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (0 + 0.25) / 4 * 100 = 6.25%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (6.25 * 0.8) = 10 + 5 = 15%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 15%
// Результат: ✓ 15%

// ============================================================
// ТЕСТ 3: Обробка 360p на 50%
// ============================================================
completedQualities = 0;
currentQualityProgress = 50;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (0 + 0.5) / 4 * 100 = 12.5%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (12.5 * 0.8) = 10 + 10 = 20%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 20%
// Результат: ✓ 20%

// ============================================================
// ТЕСТ 4: Обробка 360p на 88%
// ============================================================
completedQualities = 0;
currentQualityProgress = 88;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (0 + 0.88) / 4 * 100 = 22%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (22 * 0.8) = 10 + 17.6 = 27.6 → 28%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 28%
// Результат: ✓ 28%

// ============================================================
// ТЕСТ 5: 360p завершено (completedQualities = 1) + 480p на 12%
// ============================================================
completedQualities = 1;
currentQualityProgress = 12;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (1 + 0.12) / 4 * 100 = 28%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (28 * 0.8) = 10 + 22.4 = 32.4 → 32%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 32%
// Результат: ✓ 32%

// ============================================================
// ТЕСТ 6: 360p + 480p завершено (completedQualities = 2) + 720p на 25%
// ============================================================
completedQualities = 2;
currentQualityProgress = 25;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (2 + 0.25) / 4 * 100 = 56.25%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (56.25 * 0.8) = 10 + 45 = 55%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 55%
// Результат: ✓ 55%

// ============================================================
// ТЕСТ 7: 360p + 480p + 720p завершено (completedQualities = 3) + 1080p на 50%
// ============================================================
completedQualities = 3;
currentQualityProgress = 50;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (3 + 0.5) / 4 * 100 = 87.5%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (87.5 * 0.8) = 10 + 70 = 80%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 80%
// Результат: ✓ 80%

// ============================================================
// ТЕСТ 8: 360p + 480p + 720p завершено (completedQualities = 3) + 1080p на 100%
// ============================================================
completedQualities = 3;
currentQualityProgress = 100;

overallProgressRaw = (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
// (3 + 1.0) / 4 * 100 = 100%

overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
// 10 + (100 * 0.8) = 10 + 80 = 90%
overallProgress = Math.Clamp(overallProgress, 10, 90);
// Очікується: 90%
// Результат: ✓ 90%

// ============================================================
// ПОСЛІДОВНІСТЬ ПРОГРЕСУ (без жорсткого скидування):
// ============================================================
//
// 10% → 15% → 20% → 25% → 28% → 32% → 35% → 39% 
// → 42% → 46% → 50% → 53% → 55% → 59% → 62% → 66%
// → 70% → 73% → 75% → 78% → 80% → 82% → 85% → 88%
// → 90%
//
// ============================================================
