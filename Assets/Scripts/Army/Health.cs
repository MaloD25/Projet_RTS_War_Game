using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
	[SerializeField] float m_StartHealth;
	float m_Health;
	public float Value => m_Health;
    public float MaxValue => m_StartHealth;
    [SerializeField] Slider m_HealthBar;

	[SerializeField] UnityEvent m_OnDieEvent;

	private void Start()
	{
		m_Health = m_StartHealth;
		RefreshHealthDisplay();
	}

	void RefreshHealthDisplay()
	{
		m_HealthBar.value = m_Health / m_StartHealth;
	}
    public void Heal(float amount)
    {
        if (amount <= 0f || m_Health <= 0f)
            return;

        m_Health = Mathf.Min(
            m_Health + amount,
            m_StartHealth
        );

        RefreshHealthDisplay();
    }
    public void InflictDamage(float damage)
	{
		m_Health = Mathf.Max(m_Health - damage, 0);
		RefreshHealthDisplay();

		if (m_Health == 0 && m_OnDieEvent != null) m_OnDieEvent.Invoke();
	}
}
