using LabFusion.Player;
using LabFusion.Senders;
using LabFusion.Utilities;

namespace LabFusion.Network;

public class PlayerMetadataRequestMessage : NativeMessageHandler
{
    public override byte Tag => NativeMessageTag.PlayerMetadataRequest;

    public override ExpectedReceiverType ExpectedReceiver => ExpectedReceiverType.ServerOnly;

    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<PlayerMetadataData>();

        // Make sure the message sender is able to modify this player's metadata
        if (!NetworkVerification.HasAuthorityOverPlayer(data.Player.ID, received.Sender))
        {
            var descriptor = received.PlatformID.HasValue ? $"{received.PlatformID}" : "with no PlatformID";
            FusionLogger.Warn($"User {descriptor} attempted to modify metadata for player {data.Player.ID}!");
            return;
        }

        // Add logging for PermissionLevel metadata changes
        if (data.Key == "PermissionLevel")
        {
            var newLevel = data.Value;
            var currentLevel = data.Player.GetPlayer()?.Metadata.PermissionLevel.GetValue() ?? "unknown";
            FusionLogger.Log($"Player {received.Sender} metadata: PermissionLevel change {currentLevel} → {newLevel}");
        }

        // Block clients from elevating their own permissions
        if (data.Key == "PermissionLevel" && received.Sender != PlayerIDManager.HostSmallID)
        {
            FusionLogger.Warn($"Blocked permission elevation attempt by player {received.Sender}");
            return;
        }

        // Send the response to all clients
        PlayerSender.SendPlayerMetadataResponse(data.Player.ID, data.Key, data.Value);
    }
}
