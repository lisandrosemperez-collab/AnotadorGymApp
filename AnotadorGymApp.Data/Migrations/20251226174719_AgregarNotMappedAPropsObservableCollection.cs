using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnotadorGymApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNotMappedAPropsObservableCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodyParts",
                columns: table => new
                {
                    BodyPartId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BodyPart = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyParts", x => x.BodyPartId);
                });

            migrationBuilder.CreateTable(
                name: "Muscles",
                columns: table => new
                {
                    MuscleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Muscles", x => x.MuscleId);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    RutinaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Completado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageSource = table.Column<string>(type: "TEXT", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    TiempoPorSesion = table.Column<string>(type: "TEXT", nullable: true),
                    Dificultad = table.Column<string>(type: "TEXT", nullable: true),
                    FrecuenciaPorGrupo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.RutinaId);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutDay",
                columns: table => new
                {
                    DayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Volumen = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutDay", x => x.DayId);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    primaryMuscleId = table.Column<int>(type: "INTEGER", nullable: false),
                    bodyPartId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Mejor = table.Column<double>(type: "REAL", nullable: true),
                    Iniciar = table.Column<double>(type: "REAL", nullable: true),
                    Ultimo = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ExerciseId);
                    table.ForeignKey(
                        name: "FK_Exercises_BodyParts_bodyPartId",
                        column: x => x.bodyPartId,
                        principalTable: "BodyParts",
                        principalColumn: "BodyPartId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exercises_Muscles_primaryMuscleId",
                        column: x => x.primaryMuscleId,
                        principalTable: "Muscles",
                        principalColumn: "MuscleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RutinaSemanas",
                columns: table => new
                {
                    SemanaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RutinaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Completado = table.Column<bool>(type: "INTEGER", nullable: false),
                    NombreSemana = table.Column<string>(type: "TEXT", nullable: false),
                    SemanaIdUI = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaSemanas", x => x.SemanaId);
                    table.ForeignKey(
                        name: "FK_RutinaSemanas_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "RutinaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSecondaryMuscles",
                columns: table => new
                {
                    ExercisesAsSecundaryExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    secondaryMusclesMuscleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSecondaryMuscles", x => new { x.ExercisesAsSecundaryExerciseId, x.secondaryMusclesMuscleId });
                    table.ForeignKey(
                        name: "FK_ExerciseSecondaryMuscles_Exercises_ExercisesAsSecundaryExerciseId",
                        column: x => x.ExercisesAsSecundaryExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseSecondaryMuscles_Muscles_secondaryMusclesMuscleId",
                        column: x => x.secondaryMusclesMuscleId,
                        principalTable: "Muscles",
                        principalColumn: "MuscleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExercisesLogs",
                columns: table => new
                {
                    ExerciseLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutDayId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExercisesLogs", x => x.ExerciseLogId);
                    table.ForeignKey(
                        name: "FK_ExercisesLogs_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExercisesLogs_WorkoutDay_WorkoutDayId",
                        column: x => x.WorkoutDayId,
                        principalTable: "WorkoutDay",
                        principalColumn: "DayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaDias",
                columns: table => new
                {
                    DiaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiaIdUI = table.Column<int>(type: "INTEGER", nullable: false),
                    SemanaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Completado = table.Column<bool>(type: "INTEGER", nullable: false),
                    NombreRutinaDia = table.Column<string>(type: "TEXT", nullable: false),
                    Notas = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaDias", x => x.DiaId);
                    table.ForeignKey(
                        name: "FK_RutinaDias_RutinaSemanas_SemanaId",
                        column: x => x.SemanaId,
                        principalTable: "RutinaSemanas",
                        principalColumn: "SemanaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SetLogs",
                columns: table => new
                {
                    SetLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseLogId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kilos = table.Column<double>(type: "REAL", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetLogs", x => x.SetLogId);
                    table.ForeignKey(
                        name: "FK_SetLogs_ExercisesLogs_ExerciseLogId",
                        column: x => x.ExerciseLogId,
                        principalTable: "ExercisesLogs",
                        principalColumn: "ExerciseLogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaEjercicios",
                columns: table => new
                {
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Completado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaEjercicios", x => x.EjercicioId);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_RutinaDias_DiaId",
                        column: x => x.DiaId,
                        principalTable: "RutinaDias",
                        principalColumn: "DiaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaSeries",
                columns: table => new
                {
                    SerieId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Descanso = table.Column<string>(type: "TEXT", nullable: true),
                    Repeticiones = table.Column<int>(type: "INTEGER", nullable: true),
                    Porcentaje1RM = table.Column<int>(type: "INTEGER", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoSerie = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaSeries", x => x.SerieId);
                    table.ForeignKey(
                        name: "FK_RutinaSeries_RutinaEjercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "RutinaEjercicios",
                        principalColumn: "EjercicioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_bodyPartId",
                table: "Exercises",
                column: "bodyPartId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_primaryMuscleId",
                table: "Exercises",
                column: "primaryMuscleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSecondaryMuscles_secondaryMusclesMuscleId",
                table: "ExerciseSecondaryMuscles",
                column: "secondaryMusclesMuscleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExercisesLogs_ExerciseId",
                table: "ExercisesLogs",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExercisesLogs_WorkoutDayId",
                table: "ExercisesLogs",
                column: "WorkoutDayId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaDias_SemanaId",
                table: "RutinaDias",
                column: "SemanaId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_DiaId",
                table: "RutinaEjercicios",
                column: "DiaId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_ExerciseId",
                table: "RutinaEjercicios",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaSemanas_RutinaId",
                table: "RutinaSemanas",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaSeries_EjercicioId",
                table: "RutinaSeries",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_SetLogs_ExerciseLogId",
                table: "SetLogs",
                column: "ExerciseLogId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutDay_Date",
                table: "WorkoutDay",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseSecondaryMuscles");

            migrationBuilder.DropTable(
                name: "RutinaSeries");

            migrationBuilder.DropTable(
                name: "SetLogs");

            migrationBuilder.DropTable(
                name: "RutinaEjercicios");

            migrationBuilder.DropTable(
                name: "ExercisesLogs");

            migrationBuilder.DropTable(
                name: "RutinaDias");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "WorkoutDay");

            migrationBuilder.DropTable(
                name: "RutinaSemanas");

            migrationBuilder.DropTable(
                name: "BodyParts");

            migrationBuilder.DropTable(
                name: "Muscles");

            migrationBuilder.DropTable(
                name: "Rutinas");
        }
    }
}
