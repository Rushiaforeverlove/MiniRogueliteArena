namespace AbyssBar.Upgrades
{
    // Optional adapter implemented by an attack style. UI remains weapon-agnostic.
    public interface IUpgradeCardDescription
    { string DescribeUpgrade(UpgradeCardData card); }
}
