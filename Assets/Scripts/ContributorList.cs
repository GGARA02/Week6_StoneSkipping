using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Contributors", menuName = "ScriptableObject/Contributor", order = 0)]
[System.Serializable]
public class ContributorList : ScriptableObject
{
    public List<string> contributorList;
}