using Assingment_BeeLingua.BLL.Services;
using Assingment_BeeLingua.DAL.Models;
using Moq;
using Nexus.Base.CosmosDBRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Assingment_BeeLingua.BLL.Test.Tests
{
    public class LessonServiceTest
    {
        readonly static IEnumerable<Lesson> lessonList = new List<Lesson>
        {
            {new Lesson() { Id = "1", Description = "abcd"} },
            {new Lesson() { Id = "2", Description = "xyz0"} }
        };
        public class GetLessonById
        {
            [Theory]
            [InlineData("1")]
            [InlineData("")]
            public async Task GetDataById_ReusltFound(string id)
            {
                var repo = new Mock<IDocumentDBRepository<Lesson>>();

                //IEnumerable<Lesson> lessons = new List<Lesson>
                //{
                //    {new Lesson() { Id = "1", Description = "abcd"} },
                //    {new Lesson() { Id = "2", Description = "xyz0"} }
                //};

                var lessonData = lessonList.Where(o => o.Id == id).First();

                repo.Setup(c => c.GetByIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()
                )).Returns(
                    Task.FromResult<Lesson>(lessonData)
                );

                var svc = new LessonService(repo.Object);

                var act = await svc.GetLessonById("", null);
                Assert.Equal(lessonData, act);
            }
        }

        public class GetLessonAll
        {
            [Fact]
            public async Task GetLessonAll_Success()
            {
                var repo = new Mock<IDocumentDBRepository<Lesson>>();

                repo.Setup(c => c.GetAsync(
                    It.IsAny<Expression<Func<Lesson, bool>>>(),
                    It.IsAny<Func<IQueryable<Lesson>, IOrderedQueryable<Lesson>>>(),
                    It.IsAny<Expression<Func<Lesson, Lesson>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Dictionary<string, string>>()
                )).Returns(Task.FromResult(new PageResult<Lesson>(lessonList, "")));

                var svc = new LessonService(repo.Object);

                // act
                var actual = await svc.GetLessonAll();

                // assert
                Assert.Equal(lessonList, actual);
            }
        }
        public class CreateLesson
        {
            [Fact]
            public async Task CreateLesson_Created()
            {
                var repo = new Mock<IDocumentDBRepository<Lesson>>();

                var lessons = new Lesson
                {
                    Id = "1",
                    LessonCode = "lCOde",
                    Description = "abcd"
                };

                repo.Setup(c => c.CreateAsync(
                    It.IsAny<Lesson>(),
                    It.IsAny<EventGridOptions>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                    )).Returns(Task.FromResult(lessons));

                var svc = new LessonService(repo.Object);

                var act = await svc.CreateLesson(lessons);
                Assert.Equal(lessons, act);

            }
        }

        public class UpdateLesson
        {
            [Theory]
            [InlineData("1")]
            public async Task UpdateLesson_Updated(string id)
            {
                var repo = new Mock<IDocumentDBRepository<Lesson>>();

                var lessons = new Lesson
                {
                    Id = "1",
                    LessonCode = "lcode",
                    Description = "abcd"
                };

                repo.Setup(c => c.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Lesson>(),
                    It.IsAny<EventGridOptions>(),
                    It.IsAny<string>()
                    )).Returns(Task.FromResult(lessons));

                var svc = new LessonService(repo.Object);

                var act = await svc.UpdateLesson(id, lessons);
                Assert.Equal(lessons, act);

            }
        }

        // TODO: tambahin utk method GetLessonAll, DeleteLesson
        // TODO: implement jg unit test mock tgl 07-april yang d demoin sama rizky

    }
}
