using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MoneyMate.ViewModels;

/// <summary>
/// Classe de base pour tous les ViewModels
/// Implemente INotifyPropertyChanged pour le binding MVVM
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
	private bool _isBusy;
	private string _title = string.Empty;

	/// <summary>
	/// Indique si le ViewModel est en cours de traitement
	/// </summary>
	public bool IsBusy
	{
		get => _isBusy;
		set
		{
			if (SetProperty(ref _isBusy, value))
			{
				OnPropertyChanged(nameof(IsNotBusy));
			}
		}
	}

	/// <summary>
	/// Inverse de IsBusy pour faciliter les bindings
	/// </summary>
	public bool IsNotBusy => !IsBusy;

	/// <summary>
	/// Titre de la page/vue
	/// </summary>
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	/// <summary>
	/// Evenement declenche quand une propriete change
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Declenche levenement PropertyChanged
	/// </summary>
	/// <param name="propertyName">Nom de la propriete modifiee</param>
	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Definit une propriete et declenche PropertyChanged si la valeur change
	/// </summary>
	/// <typeparam name="T">Type de la propriete</typeparam>
	/// <param name="field">Reference vers le champ de stockage</param>
	/// <param name="value">Nouvelle valeur</param>
	/// <param name="propertyName">Nom de la propriete</param>
	/// <returns>True si la valeur a change, false sinon</returns>
	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}