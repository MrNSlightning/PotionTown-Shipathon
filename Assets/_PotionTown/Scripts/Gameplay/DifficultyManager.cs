using UnityEngine;

namespace PotionShop
{
    /// <summary>
    /// Gün bazlı zorluk parametrelerini hesaplayan statik yardımcı sınıf.
    /// 70 günlük bir zorluk eğrisi tanımlar.
    /// </summary>
    public static class DifficultyManager
    {
        // Zorluk aşamaları
        public enum DifficultyPhase
        {
            Baslangic,  // Gün 1-10
            Gelisme,    // Gün 11-25
            Zor,        // Gün 26-40
            CokZor,     // Gün 41-55
            Efsanevi    // Gün 56-70
        }

        /// <summary>
        /// Günün zorluk aşamasını döndürür.
        /// </summary>
        public static DifficultyPhase GetPhase(int day)
        {
            if (day <= 10) return DifficultyPhase.Baslangic;
            if (day <= 25) return DifficultyPhase.Gelisme;
            if (day <= 40) return DifficultyPhase.Zor;
            if (day <= 55) return DifficultyPhase.CokZor;
            return DifficultyPhase.Efsanevi;
        }

        /// <summary>
        /// Zorluk aşamasının Türkçe adını döndürür.
        /// </summary>
        public static string GetDifficultyName(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return "Başlangıç";
                case DifficultyPhase.Gelisme:   return "Gelişme";
                case DifficultyPhase.Zor:       return "Zor";
                case DifficultyPhase.CokZor:    return "Çok Zor";
                case DifficultyPhase.Efsanevi:  return "Efsanevi";
                default:                        return "Bilinmiyor";
            }
        }

        /// <summary>
        /// Müşterinin her ruh hali aşaması için bekleme süresi (saniye).
        /// Zorluk burada artar: Düşük = daha sabırsız müşteri.
        /// </summary>
        public static float GetTimePerMood(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 18f; // Çok sabırlı
                case DifficultyPhase.Gelisme:   return 14f;
                case DifficultyPhase.Zor:       return 10f;
                case DifficultyPhase.CokZor:    return 7f;
                case DifficultyPhase.Efsanevi:  return 4.5f; // Çok sabırsız! Hızlı olmalı
                default:                        return 18f;
            }
        }

        /// <summary>
        /// Doğru iksir teslim edildiğinde müşteriye eklenen bonus sabır süresi (saniye).
        /// </summary>
        public static float GetDeliveryBonusTime(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 20f;
                case DifficultyPhase.Gelisme:   return 15f;
                case DifficultyPhase.Zor:       return 12f;
                case DifficultyPhase.CokZor:    return 8f;
                case DifficultyPhase.Efsanevi:  return 5f;
                default:                        return 20f;
            }
        }

        /// <summary>
        /// Önceki müşteri gittikten sonra yenisinin gelmesi için geçecek MİNİMUM süre (saniye).
        /// Sürekli müşteri gelmesi için çok düşük tutuldu.
        /// </summary>
        public static float GetMinSpawnDelay(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 3f;
                case DifficultyPhase.Gelisme:   return 2f;
                case DifficultyPhase.Zor:       return 2f;
                case DifficultyPhase.CokZor:    return 1f;
                case DifficultyPhase.Efsanevi:  return 1f;
                default:                        return 3f;
            }
        }

        /// <summary>
        /// Önceki müşteri gittikten sonra yenisinin gelmesi için geçecek MAKSİMUM süre (saniye).
        /// </summary>
        public static float GetMaxSpawnDelay(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 6f;
                case DifficultyPhase.Gelisme:   return 5f;
                case DifficultyPhase.Zor:       return 4f;
                case DifficultyPhase.CokZor:    return 3f;
                case DifficultyPhase.Efsanevi:  return 2f;
                default:                        return 6f;
            }
        }

        /// <summary>
        /// Bir müşterinin isteyebileceği maksimum iksir sayısı.
        /// </summary>
        public static int GetMaxOrderCount(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 1;
                case DifficultyPhase.Gelisme:   return 2;
                case DifficultyPhase.Zor:       return 2;
                case DifficultyPhase.CokZor:    return 3;
                case DifficultyPhase.Efsanevi:  return 3;
                default:                        return 1;
            }
        }

        /// <summary>
        /// Bir müşterinin isteyeceği minimum iksir sayısı.
        /// </summary>
        public static int GetMinOrderCount(int day)
        {
            switch (GetPhase(day))
            {
                case DifficultyPhase.Baslangic: return 1;
                case DifficultyPhase.Gelisme:   return 1;
                case DifficultyPhase.Zor:       return 1;
                case DifficultyPhase.CokZor:    return 2;
                case DifficultyPhase.Efsanevi:  return 2;
                default:                        return 1;
            }
        }
    }
}
