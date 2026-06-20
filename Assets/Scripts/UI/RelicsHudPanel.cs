using UnityEngine;

// DEPRECATED — KALINTILAR paneli artık MainHudCanvasUI içine gerçek HUD parçası
// olarak entegre edildi (BuildRelicsHud / UpdateRelicsHud).
//
// Bu sınıf eskiden ayrı bir ScreenSpaceOverlay Canvas oluşturuyordu; bu yaklaşım
// gameplay HUD'una entegre olmadığı için bırakıldı. AutoCreate devre dışı.
// Dosya/meta referansları bozulmasın diye sınıf korunuyor ama hiçbir şey yapmıyor.
public class RelicsHudPanel : MonoBehaviour
{
    // RuntimeInitializeOnLoadMethod KASITLI OLARAK KALDIRILDI — otomatik oluşturma yok.
    public static void EnsureInstance() { /* no-op: MainHudCanvasUI yönetir */ }
}
