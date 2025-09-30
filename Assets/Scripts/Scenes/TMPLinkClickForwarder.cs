/*
============================================================
TMPLinkClickForwarder.cs — Reenvío de clicks en enlaces TMP
------------------------------------------------------------
PROPÓSITO
- Capturar clicks sobre <link="..."> de TextMeshPro y notificar
  al controlador (p.ej., abrir GlossaryPanel).

USO
- Asignar al TMP_Text correspondiente.

MÉTODOS (COMPLETA AQUÍ)
- OnPointerClick: obtiene linkID y lo envía a un callback (Glossary).
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;


public class TMPLinkClickForwarder : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text _text;
    private System.Action<string, string> _onLink;

    public void Bind(TMP_Text text, System.Action<string, string> onLink)
    {
        _text = text;
        _onLink = onLink;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, eventData.pressEventCamera);
        if (linkIndex != -1)
        {
            var info = _text.textInfo.linkInfo[linkIndex];
            _onLink?.Invoke(info.GetLinkID(), info.GetLinkText());
        }
    }
}
