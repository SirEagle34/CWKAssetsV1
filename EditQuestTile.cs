using UnityEngine;

/// <summary>
/// Minimal quest tile used only by EditCustomDungeon02.
/// The prefab needs only a SelectedLabel object and a QuestName UILabel.
/// </summary>
public class EditQuestTile : UIStreamingGridListItem
{
    public GameObject SelectedLabel;
    public UILabel QuestName;

    private CustomQuestController.QuestEntry mEntry;
    private EditCustomDungeon02 mEditor;
    private Color mDefaultNameColor = Color.white;

    private void Awake()
    {
        if (QuestName != null) mDefaultNameColor = QuestName.color;
        mEditor = NGUITools.FindInParents<EditCustomDungeon02>(transform);
    }

    public override void Populate(object dataObj)
    {
        mEntry = dataObj as CustomQuestController.QuestEntry;
        if (mEditor == null) mEditor = NGUITools.FindInParents<EditCustomDungeon02>(transform);

        if (mEntry == null)
        {
            if (QuestName != null) QuestName.text = string.Empty;
            if (SelectedLabel != null) SelectedLabel.SetActive(false);
            return;
        }

        if (QuestName != null)
        {
            QuestName.text = mEntry.Name;
            QuestName.color = mDefaultNameColor;
        }
        RefreshSelectedState();
    }

    public void RefreshSelectedState()
    {
        bool selected = mEditor != null && mEntry != null && mEditor.IsQuestSelected(mEntry);
        if (SelectedLabel != null) SelectedLabel.SetActive(selected);
        if (QuestName != null) QuestName.color = selected ? Color.gray : mDefaultNameColor;
    }

    public void OnClick()
    {
        if (mEntry == null) return;
        if (mEditor == null) mEditor = NGUITools.FindInParents<EditCustomDungeon02>(transform);
        if (mEditor != null) mEditor.SelectQuest(mEntry);
    }

    public override void Unload()
    {
        base.Unload();
        mEntry = null;
    }
}
