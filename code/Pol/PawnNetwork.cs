using Sandbox;

public sealed class PawnNetwork : BaseComponent
{
	[Property] public GameObject ObjectToSpawn { get; set; }

	public override void OnStart()
	{
		base.OnStart();

		Log.Info( "OnStart" );

		var spawnPoint = Scene.FindAllComponents<SimpleSpawnPoint>( false ).FirstOrDefault();

		var go = SceneUtility.Instantiate( ObjectToSpawn, spawnPoint.Transform.Position );
		go.Enabled = true;

		go.NetworkSpawn();

	}

	public override void Update()
	{
		Log.Info( "Update" );

	}

}
