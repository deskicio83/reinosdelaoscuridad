using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinoOscuridad.UI.HeroScene
{
    public class HeroCardView : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _nombre;
        [SerializeField] private TMP_Text _nivel;
        [SerializeField] private GameObject _favoritoIcon;
        [SerializeField] private Button _button;

        private string _heroId;
        private Action<string> _onClick;

        public void Bind(string heroId, string nombre, int nivel, Sprite portrait, bool favorito, Action<string> onClick)
        {
            _heroId = heroId;
            _onClick = onClick;

            if (_portrait != null) _portrait.sprite = portrait;
            if (_nombre != null) _nombre.text = nombre;
            if (_nivel != null) _nivel.text = "Nv. " + nivel;
            if (_favoritoIcon != null) _favoritoIcon.SetActive(favorito);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onClick?.Invoke(_heroId));
            }
        }

        public void BindBuySlot(string label, Action onClick)
        {
            _heroId = null;

            if (_portrait != null) _portrait.sprite = null;
            if (_nombre != null) _nombre.text = label;
            if (_nivel != null) _nivel.text = string.Empty;
            if (_favoritoIcon != null) _favoritoIcon.SetActive(false);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        public Image Portrait => _portrait;
        public TMP_Text Nombre => _nombre;
        public TMP_Text Nivel => _nivel;
        public GameObject FavoritoIcon => _favoritoIcon;
        public Button CardButton => _button;
    }
}
