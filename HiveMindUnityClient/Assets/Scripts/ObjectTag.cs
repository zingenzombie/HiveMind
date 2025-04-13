using UnityEngine;

public class ObjectTag
{
    string objectHash = "";

    public bool setObjectHash(string hash)
    {
        if (objectHash == null)
            return false;

        objectHash = hash;
        return true;
    }

    public string getObjectHash()
    {
        return objectHash;
    }
}
