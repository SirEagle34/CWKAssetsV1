using UnityEngine;
using System;
using System.Collections.Generic;

public class ChangeLogPopupUI : MonoBehaviour
{
    public static ChangeLogPopupUI Instance;

    [Header("Root Popup")]
    public GameObject Root; // bütün popup paneli

    [Header("Header")]
    public UILabel Title;
    public UILabel Version;

    [Header("Lines")]
    public UILabel Line1;
    public UILabel Line2;
    public UILabel Line3;
    public UILabel Line4;
    public UILabel Line5;
    public UILabel Line6;
    public UILabel Line7;

    private bool isOpen;

    private void Awake()
    {
        Instance = this;

        if (Root != null)
            Root.SetActive(false);

        isOpen = false;
    }

    // =========================
    // SHOW
    // =========================
    public void Show(ChangeLogData data)
    {
        if (data == null)
            return;

        Bind(data);
        Open();
    }

    // =========================
    // HIDE
    // =========================
    public void Hide()
    {
        if (!isOpen)
            return;

        if (Root != null)
            Root.SetActive(false);

        isOpen = false;
    }

    // =========================
    // OPEN INTERNAL
    // =========================
    void Open()
    {
        if (isOpen)
            return;

        if (Root != null)
            Root.SetActive(true);

        isOpen = true;
    }

    // =========================
    // BIND DATA
    // =========================
    void Bind(ChangeLogData data)
    {
        Clear();

        if (Title != null) Title.text = data.Title;
        if (Version != null) Version.text = data.Version;

        if (Line1 != null) Line1.text = data.Line1;
        if (Line2 != null) Line2.text = data.Line2;
        if (Line3 != null) Line3.text = data.Line3;
        if (Line4 != null) Line4.text = data.Line4;
        if (Line5 != null) Line5.text = data.Line5;
        if (Line6 != null) Line6.text = data.Line6;
        if (Line7 != null) Line7.text = data.Line7;
    }

    // =========================
    // CLEAR
    // =========================
    void Clear()
    {
        if (Title != null) Title.text = "";
        if (Version != null) Version.text = "";

        if (Line1 != null) Line1.text = "";
        if (Line2 != null) Line2.text = "";
        if (Line3 != null) Line3.text = "";
        if (Line4 != null) Line4.text = "";
        if (Line5 != null) Line5.text = "";
        if (Line6 != null) Line6.text = "";
        if (Line7 != null) Line7.text = "";
    }

    public void ShowLatest()
    {
        ChangeLogData data = ChangeLogDataManager.Instance.GetLatest();

        if (data == null)
        {
            Debug.LogError("No changelog data found!");
            return;
        }

        Show(data);
    }
    
	public void OnClose()
	{
		Hide();
		// burada ileride: PlayCloseAnimation();
	}
}