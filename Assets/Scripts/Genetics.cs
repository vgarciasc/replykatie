using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Genetics
{
    public static float CombineGenes(float gene_1, float gene_2) {
        int coin = Random.Range(0, 5);

        switch (coin) {
            case 0: return gene_1;
            case 1: return gene_2;
            case 2: return Random.Range(
                Mathf.Min(gene_1, gene_2),
                Mathf.Max(gene_1, gene_2));
            case 3: return gene_1 + Random.Range(
                - gene_1, 
                + gene_1);
            case 4: return gene_2 + Random.Range(
                - gene_2, 
                + gene_2);
            default: 
                Debug.Log("This should not be happening.");
                Debug.Break();
                return -1;
        }
    }
}
