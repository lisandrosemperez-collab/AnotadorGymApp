using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnotadorGymApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArregloCascadeDeleteForSetLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SetLogs_ExercisesLogs_ExerciseLogId",
                table: "SetLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_SetLogs_ExercisesLogs_ExerciseLogId",
                table: "SetLogs",
                column: "ExerciseLogId",
                principalTable: "ExercisesLogs",
                principalColumn: "ExerciseLogId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SetLogs_ExercisesLogs_ExerciseLogId",
                table: "SetLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_SetLogs_ExercisesLogs_ExerciseLogId",
                table: "SetLogs",
                column: "ExerciseLogId",
                principalTable: "ExercisesLogs",
                principalColumn: "ExerciseLogId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
