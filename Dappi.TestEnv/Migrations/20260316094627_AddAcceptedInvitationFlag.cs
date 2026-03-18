using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dappi.TestEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedInvitationFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptedInvitation",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedInvitation",
                table: "AspNetUsers");
        }
    }
}
