using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SeedController : ControllerBase
{
    private readonly SkillSnapContext _context;

    public SeedController(SkillSnapContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (await _context.PortfolioUsers.AnyAsync())
        {
            return BadRequest("Sample data already exists.");
        }

        var users = new List<PortfolioUser>
        {
            // User 1: Jordan Developer - Full-stack
            new PortfolioUser
            {
                Name = "Jordan Developer",
                Bio = "Full-stack developer passionate about learning new tech.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Task Tracker",
                        Description = "Manage tasks effectively with a modern UI",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Weather App",
                        Description = "Forecast weather using external APIs",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "E-commerce Platform",
                        Description = "Online shopping platform with payment integration",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "C#", Level = "Advanced" },
                    new Skill { Name = "Blazor", Level = "Intermediate" },
                    new Skill { Name = "ASP.NET Core", Level = "Advanced" },
                    new Skill { Name = "Entity Framework", Level = "Intermediate" }
                }
            },

            // User 2: Sarah Chen - Frontend Specialist
            new PortfolioUser
            {
                Name = "Sarah Chen",
                Bio = "Frontend developer specializing in React and modern JavaScript frameworks.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Portfolio Website",
                        Description = "Responsive portfolio site with animations and modern design",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Social Media Dashboard",
                        Description = "Real-time analytics dashboard for social media metrics",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Blog Platform",
                        Description = "Content management system with rich text editor",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Landing Page Builder",
                        Description = "Drag-and-drop tool for creating landing pages",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "React", Level = "Advanced" },
                    new Skill { Name = "TypeScript", Level = "Advanced" },
                    new Skill { Name = "CSS/SASS", Level = "Advanced" },
                    new Skill { Name = "JavaScript", Level = "Advanced" },
                    new Skill { Name = "Tailwind CSS", Level = "Intermediate" }
                }
            },

            // User 3: Marcus Johnson - Backend Engineer
            new PortfolioUser
            {
                Name = "Marcus Johnson",
                Bio = "Backend engineer focused on scalable APIs and microservices architecture.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "RESTful API Gateway",
                        Description = "High-performance API gateway with rate limiting and caching",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Inventory Management System",
                        Description = "Real-time inventory tracking for warehouse operations",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Payment Processing Service",
                        Description = "Secure payment integration with multiple providers",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Authentication Service",
                        Description = "OAuth2 and JWT-based authentication microservice",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Email Notification System",
                        Description = "Scalable email service with templating and scheduling",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Node.js", Level = "Advanced" },
                    new Skill { Name = "Python", Level = "Advanced" },
                    new Skill { Name = "PostgreSQL", Level = "Advanced" },
                    new Skill { Name = "Redis", Level = "Intermediate" },
                    new Skill { Name = "Docker", Level = "Advanced" },
                    new Skill { Name = "MongoDB", Level = "Intermediate" }
                }
            },

            // User 4: Elena Rodriguez - DevOps Engineer
            new PortfolioUser
            {
                Name = "Elena Rodriguez",
                Bio = "DevOps engineer passionate about automation and cloud infrastructure.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "CI/CD Pipeline",
                        Description = "Automated deployment pipeline with testing and monitoring",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Infrastructure as Code",
                        Description = "Terraform scripts for multi-cloud deployment",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Monitoring Dashboard",
                        Description = "Real-time system monitoring with Grafana and Prometheus",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "AWS", Level = "Advanced" },
                    new Skill { Name = "Kubernetes", Level = "Advanced" },
                    new Skill { Name = "Terraform", Level = "Intermediate" },
                    new Skill { Name = "Jenkins", Level = "Intermediate" }
                }
            },

            // User 5: Raj Patel - Mobile Developer
            new PortfolioUser
            {
                Name = "Raj Patel",
                Bio = "Mobile developer creating beautiful cross-platform applications.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Fitness Tracker App",
                        Description = "Track workouts, calories, and fitness goals",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Restaurant Finder",
                        Description = "Discover local restaurants with reviews and reservations",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Budget Manager",
                        Description = "Personal finance app with expense tracking",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Language Learning App",
                        Description = "Interactive lessons for learning new languages",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Flutter", Level = "Advanced" },
                    new Skill { Name = "Dart", Level = "Advanced" },
                    new Skill { Name = "React Native", Level = "Intermediate" },
                    new Skill { Name = "Swift", Level = "Intermediate" },
                    new Skill { Name = "Firebase", Level = "Advanced" }
                }
            },

            // User 6: Kenji Tanaka - Data Scientist
            new PortfolioUser
            {
                Name = "Kenji Tanaka",
                Bio = "Data scientist leveraging machine learning to solve complex problems.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Predictive Analytics Dashboard",
                        Description = "ML-powered sales forecasting and trend analysis",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Image Classification Model",
                        Description = "CNN-based image recognition for medical diagnostics",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Recommendation Engine",
                        Description = "Collaborative filtering for personalized product recommendations",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Python", Level = "Advanced" },
                    new Skill { Name = "TensorFlow", Level = "Advanced" },
                    new Skill { Name = "Pandas", Level = "Advanced" },
                    new Skill { Name = "SQL", Level = "Advanced" },
                    new Skill { Name = "R", Level = "Intermediate" },
                    new Skill { Name = "Scikit-learn", Level = "Advanced" }
                }
            },

            // User 7: Olivia Brown - UX/UI Designer
            new PortfolioUser
            {
                Name = "Olivia Brown",
                Bio = "UX/UI designer with front-end development skills, creating delightful user experiences.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Banking App Redesign",
                        Description = "Modern interface for mobile banking application",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Design System",
                        Description = "Comprehensive component library and style guide",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "E-learning Platform UI",
                        Description = "Intuitive interface for online course platform",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Dashboard Prototype",
                        Description = "Interactive prototype for analytics dashboard",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Figma", Level = "Advanced" },
                    new Skill { Name = "HTML/CSS", Level = "Advanced" },
                    new Skill { Name = "Adobe XD", Level = "Intermediate" },
                    new Skill { Name = "JavaScript", Level = "Intermediate" }
                }
            },

            // User 8: Ahmed Hassan - Security Specialist
            new PortfolioUser
            {
                Name = "Ahmed Hassan",
                Bio = "Cybersecurity specialist focused on application security and penetration testing.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Security Audit Tool",
                        Description = "Automated vulnerability scanning for web applications",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Encryption Library",
                        Description = "Open-source cryptography library for secure data handling",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Intrusion Detection System",
                        Description = "Real-time network monitoring and threat detection",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Secure API Gateway",
                        Description = "API security with OAuth2, rate limiting, and encryption",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Password Manager",
                        Description = "Secure password vault with multi-factor authentication",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Penetration Testing", Level = "Advanced" },
                    new Skill { Name = "Python", Level = "Advanced" },
                    new Skill { Name = "Network Security", Level = "Advanced" },
                    new Skill { Name = "Cryptography", Level = "Advanced" }
                }
            },

            // User 9: Nina Petrov - Cloud Architect
            new PortfolioUser
            {
                Name = "Nina Petrov",
                Bio = "Cloud architect designing scalable and resilient cloud solutions.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Multi-Region Deployment",
                        Description = "Global application deployment with auto-failover",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Serverless Architecture",
                        Description = "Cost-effective serverless solution using AWS Lambda",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Cloud Migration Strategy",
                        Description = "Enterprise migration from on-premise to cloud",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "AWS", Level = "Advanced" },
                    new Skill { Name = "Azure", Level = "Advanced" },
                    new Skill { Name = "Microservices", Level = "Advanced" },
                    new Skill { Name = "Serverless", Level = "Advanced" },
                    new Skill { Name = "Cloud Security", Level = "Intermediate" }
                }
            },

            // User 10: Carlos Santos - Game Developer
            new PortfolioUser
            {
                Name = "Carlos Santos",
                Bio = "Game developer creating immersive gaming experiences for multiple platforms.",
                ProfileImageUrl = "",
                Projects = new List<Project>
                {
                    new Project
                    {
                        Title = "Action RPG",
                        Description = "3D role-playing game with combat and exploration",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Puzzle Mobile Game",
                        Description = "Addictive puzzle game with 100+ levels",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "Multiplayer Arena",
                        Description = "Online multiplayer battle arena with matchmaking",
                        ImageUrl = ""
                    },
                    new Project
                    {
                        Title = "VR Experience",
                        Description = "Virtual reality adventure game for VR headsets",
                        ImageUrl = ""
                    }
                },
                Skills = new List<Skill>
                {
                    new Skill { Name = "Unity", Level = "Advanced" },
                    new Skill { Name = "C#", Level = "Advanced" },
                    new Skill { Name = "Unreal Engine", Level = "Intermediate" },
                    new Skill { Name = "3D Modeling", Level = "Intermediate" },
                    new Skill { Name = "Game Design", Level = "Advanced" }
                }
            }
        };

        _context.PortfolioUsers.AddRange(users);
        await _context.SaveChangesAsync();

        return Ok("Sample data inserted successfully.");
    }
}
