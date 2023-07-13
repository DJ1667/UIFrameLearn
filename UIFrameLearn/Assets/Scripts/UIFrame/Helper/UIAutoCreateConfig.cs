using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAutoCreateConfig : ScriptableObject
{
    [SerializeField]
    private string m_PrefabPath = "";
    [SerializeField]
    private bool m_IsSimplyName = false;
    [SerializeField]
    private bool m_IsWithExtension = false;
    [SerializeField]
    private string m_CodePath = "";
    [SerializeField]
    private string m_Namespace = "";

    public string PrefabPath
    {
        get
        {
            return m_PrefabPath;
        }
        set
        {
            m_PrefabPath = value;
        }

    }

    public bool IsSimplyName
    {
        get
        {
            return m_IsSimplyName;
        }
        set
        {
            m_IsSimplyName = value;
        }

    }

    public bool IsWithExtension
    {
        get
        {
            return m_IsWithExtension;
        }
        set
        {
            m_IsWithExtension = value;
        }

    }

    public string CodePath
    {
        get
        {
            return m_CodePath;
        }
        set
        {
            m_CodePath = value;
        }

    }

    public string Namespace
    {
        get
        {
            return m_Namespace;
        }
        set
        {
            m_Namespace = value;
        }

    }
}
