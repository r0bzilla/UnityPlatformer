using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script uses a BoxCollider2D attached to the same GameObject, and
/// generates an colliders for each side of the BoxCollider2D.
/// A LayerMask can be set to filter the touch behavior of the four sides.
/// </summary>
public class QuadCollider2D : MonoBehaviour
{
    private BoxCollider2D boundingBox;
    private Collider2D right;
    private Collider2D left;
    private Collider2D top;
    private Collider2D bottom;

    [SerializeField]
    [Tooltip("The LayerMask used for checking if the colliders are touching things.")]
    public int layerMask = 0;

    public bool isTouchingRight { get; private set; }
    public bool isTouchingLeft { get; private set; }
    public bool isTouchingTop { get; private set; }
    public bool isTouchingBottom { get; private set; }


    /// <summary>
	/// Initializes the colliders.
	/// </summary>
    protected void Start()
    {
        this.boundingBox = GetComponent<BoxCollider2D>();
        GenerateColliders();
    }


    /// <summary>
	/// On each frame, checks if any of the four colliders is touching the layerMask.
	/// </summary>
    protected void Update()
    {
        this.isTouchingRight = right.IsTouchingLayers(layerMask);
        this.isTouchingLeft = left.IsTouchingLayers(layerMask);
        this.isTouchingTop = top.IsTouchingLayers(layerMask);
        this.isTouchingBottom = bottom.IsTouchingLayers(layerMask);
    }


    /// <summary>
    /// Generates colliders at the borders of the boundingBox BoxCollider2D.
    /// </summary>
    private void GenerateColliders()
    {
        float colliderWidth = 0.015625f;
        float width = boundingBox.size.x;
        float height = boundingBox.size.y;
        float offsetX = boundingBox.offset.x;
        float offsetY = boundingBox.offset.y;

        // create right collider
        BoxCollider2D rightCollider = boundingBox.gameObject.AddComponent<BoxCollider2D>();
        rightCollider.size = new Vector2(colliderWidth, height - colliderWidth * 2f);
        rightCollider.offset = new Vector2(offsetX + (width - colliderWidth) / 2f, offsetY);
        rightCollider.isTrigger = true;
        this.right = rightCollider;

        // create left collider
        BoxCollider2D leftCollider = boundingBox.gameObject.AddComponent<BoxCollider2D>();
        leftCollider.size = rightCollider.size;
        leftCollider.offset = new Vector2(offsetX - (width - colliderWidth) / 2f, offsetY);
        leftCollider.isTrigger = true;
        this.left = leftCollider;

        // create top collider
        BoxCollider2D topCollider = boundingBox.gameObject.AddComponent<BoxCollider2D>();
        topCollider.size = new Vector2(width - colliderWidth * 2f, colliderWidth);
        topCollider.offset = new Vector2(offsetX, offsetY + (height - colliderWidth) / 2f);
        topCollider.isTrigger = true;
        this.top = topCollider;

        // create bottom collider
        BoxCollider2D bottomCollider = boundingBox.gameObject.AddComponent<BoxCollider2D>();
        bottomCollider.size = topCollider.size;
        bottomCollider.offset = new Vector2(offsetX, offsetY - (height - colliderWidth) / 2f);
        bottomCollider.isTrigger = true;
        this.bottom = bottomCollider;
    }
}
