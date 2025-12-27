using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Data
{
    public class DataBase : DbContext
    {        
        public DataBase() {}        
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite("Data Source=GymApp.db"); // La base de datos SQLite
            }
        }
        public DataBase(DbContextOptions<DataBase> options) : base(options) { }                
        #region DbSetsMadres
        //Madres
        public DbSet<BodyParts> BodyParts { get; set; }        
        public DbSet<Exercise> Exercises { get;set; }        
        public DbSet<Rutinas> Rutinas { get; set; }  
        public DbSet<Muscle> Muscles { get; set; }
        public DbSet<WorkoutDay> WorkoutDay { get; set; }
        #endregion
        #region DbSetsHijas
        public DbSet<ExerciseLog> ExercisesLogs { get; set; }
        public DbSet<SetLog> SetLogs { get; set; }        
        public DbSet<RutinaSemana> RutinaSemanas { get; set; }
        public DbSet<RutinaDia> RutinaDias { get; set; }
        public DbSet<RutinaEjercicio> RutinaEjercicios { get; set; }
        public DbSet<RutinaSeries> RutinaSeries { get; set; }
        #endregion        
        #region //ModelCreating
        protected override void OnModelCreating(ModelBuilder model)
        {
            #region //Exercises //TERMINADO
            model.Entity<Exercise>(entity =>
            {
                entity.HasKey(r => r.ExerciseId);

                entity.HasMany(r => r.ExerciseLogs)
                    .WithOne(r => r.Exercise)
                    .HasForeignKey(r => r.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                #region Rutina                
                entity.HasMany(r => r.RutinasEjercicios)
                    .WithOne(r => r.Exercise)                    
                    .HasForeignKey(r => r.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);
                #endregion
                #region Muscles
                entity.HasMany(r => r.secondaryMuscles)
                    .WithMany(r => r.ExercisesAsSecundary)
                    .UsingEntity(j => j.ToTable("ExerciseSecondaryMuscles"));                                        
                
                entity.HasOne(r => r.primaryMuscle)
                    .WithMany(r => r.ExercisesAsMain)
                    .HasForeignKey(r => r.primaryMuscleId)
                    .OnDelete(DeleteBehavior.Cascade);                    
                
                entity.HasOne(r => r.bodyPart)
                    .WithMany(r => r.ExercisesList)
                    .HasForeignKey(r => r.bodyPartId)
                    .OnDelete(DeleteBehavior.Cascade);
                #endregion
            });            
            model.Entity<WorkoutDay>(entity =>
            {
                entity.HasKey(r => r.DayId);                
                entity.HasIndex(r => r.Date)
                    .IsUnique();

                entity.HasMany(r => r.ExerciseLogs)
                        .WithOne(r => r.WorkoutDay)
                        .HasForeignKey(r => r.WorkoutDayId).OnDelete(DeleteBehavior.Cascade);
                
            });
            model.Entity<ExerciseLog>(entity =>
            {
                entity.HasKey(r => r.ExerciseLogId);

                entity.HasMany(r => r.SetsLog)
                    .WithOne(r => r.ExerciseLog)
                    .HasForeignKey(r => r.ExerciseLogId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.WorkoutDay)
                    .WithMany(r => r.ExerciseLogs)
                    .HasForeignKey(r => r.WorkoutDayId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Exercise)
                    .WithMany(r => r.ExerciseLogs)
                    .HasForeignKey(r => r.ExerciseId).OnDelete(DeleteBehavior.Cascade);
            });
            model.Entity<SetLog>(entity =>
            {
                entity.HasKey(r => r.SetLogId);

                entity.HasOne(r => r.ExerciseLog)
                    .WithMany(r => r.SetsLog)
                    .HasForeignKey(r => r.ExerciseLogId).OnDelete(deleteBehavior: DeleteBehavior.Cascade);  
            });
            #endregion

            #region//RUTINAS
            model.Entity<Rutinas>(entity =>
            {                
                entity.Property(r => r.Nombre).IsRequired(true);
                entity.HasKey(r => r.RutinaId);
                entity.Property(r => r.RutinaId).ValueGeneratedOnAdd();

                entity.HasMany(r => r.Semanas)
                    .WithOne(r => r.Rutina)
                    .HasForeignKey(r => r.RutinaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            model.Entity<RutinaSemana>(entity =>
            {
                entity.HasKey(r => r.SemanaId);
                entity.Property(r => r.SemanaId).ValueGeneratedOnAdd();

                entity.HasMany(r => r.Dias)
                    .WithOne(r => r.Semana)
                    .HasForeignKey(r => r.SemanaId )
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Rutina)
                    .WithMany(r => r.Semanas)
                    .HasForeignKey(rs => rs.RutinaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });                            
            model.Entity<RutinaDia>(entity =>
            {
                entity.HasKey(r => r.DiaId);
                entity.Property(r => r.DiaId).ValueGeneratedOnAdd();

                entity.HasMany(r => r.Ejercicios)
                    .WithOne(r => r.Dia)
                    .HasForeignKey(r => r.DiaId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(rd => rd.Semana)
                    .WithMany(rs => rs.Dias)
                    .HasForeignKey(rd => rd.SemanaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            model.Entity<RutinaEjercicio>(entity =>
            {
                entity.HasKey(r => r.EjercicioId );
                entity.Property(r => r.EjercicioId).ValueGeneratedOnAdd();
                
                entity.HasMany(r => r.Series)
                    .WithOne(r => r.Ejercicio)
                    .HasForeignKey(r => r.EjercicioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(re => re.Dia)
                    .WithMany(rd => rd.Ejercicios)
                    .HasForeignKey(re => re.DiaId )
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(re => re.Exercise)
                    .WithMany(re => re.RutinasEjercicios)
                    .HasForeignKey(re => re.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(re => re.Completado).HasDefaultValue(false);

            });
            model.Entity<RutinaSeries>(entity =>
            {
                entity.HasKey(r => r.SerieId );
                entity.Property(r => r.SerieId).ValueGeneratedOnAdd();

                entity.HasOne(r => r.Ejercicio)
                        .WithMany(r => r.Series)
                        .HasForeignKey(r => r.EjercicioId)
                        .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Descanso)
                        .HasConversion(
                            v => v.HasValue ? v.Value.ToString(@"hh\:mm\:ss") : "00:00:00",
                            v => v == null || v == "00:00:00" || string.IsNullOrEmpty(v) ? TimeSpan.Zero : TimeSpan.Parse(v))
                        .HasColumnType("TEXT");
            });
            #endregion

            #region//Etiquetas            
            model.Entity<BodyParts>(entity =>
            {
                entity.HasKey(r => r.BodyPartId);
                entity.HasMany(r => r.ExercisesList)
                    .WithOne(r => r.bodyPart)
                    .HasForeignKey(r => r.bodyPartId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            model.Entity<Muscle>(entity =>
            {
                entity.HasKey(r => r.MuscleId);
                entity.HasMany(r => r.ExercisesAsMain)
                    .WithOne(r => r.primaryMuscle)
                    .HasForeignKey(r => r.primaryMuscleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(r => r.ExercisesAsSecundary)
                    .WithMany(r => r.secondaryMuscles)
                    .UsingEntity(r => r.ToTable("ExerciseSecondaryMuscles"));
            });
            #endregion            
        }
        #endregion        
        

    }
}


