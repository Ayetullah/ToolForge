using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UtilityTools.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeJobUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Users_UserId",
                table: "Jobs");

            // Convert SubscriptionTier from string to integer
            // First, update existing string values to integer equivalents
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""SubscriptionTier"" = CASE
                    WHEN ""SubscriptionTier"" = 'Free' THEN 0
                    WHEN ""SubscriptionTier"" = 'Basic' THEN 1
                    WHEN ""SubscriptionTier"" = 'Pro' THEN 2
                    WHEN ""SubscriptionTier"" = 'Enterprise' THEN 3
                    WHEN ""SubscriptionTier"" = 'Admin' THEN 99
                    ELSE 0
                END
                WHERE ""SubscriptionTier"" ~ '^[A-Za-z]+$';
            ");

            // Convert SubscriptionTier from string to integer using USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE ""Users""
                ALTER COLUMN ""SubscriptionTier"" TYPE integer
                USING CASE
                    WHEN ""SubscriptionTier""::text = 'Free' THEN 0
                    WHEN ""SubscriptionTier""::text = 'Basic' THEN 1
                    WHEN ""SubscriptionTier""::text = 'Pro' THEN 2
                    WHEN ""SubscriptionTier""::text = 'Enterprise' THEN 3
                    WHEN ""SubscriptionTier""::text = 'Admin' THEN 99
                    ELSE CAST(""SubscriptionTier"" AS integer)
                END;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Jobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Users_UserId",
                table: "Jobs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Users_UserId",
                table: "Jobs");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionTier",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Users_UserId",
                table: "Jobs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
